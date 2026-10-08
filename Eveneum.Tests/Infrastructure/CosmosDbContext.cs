using Eveneum.Documents;
using Eveneum.Snapshots;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eveneum.Tests.Infrastructure;

public abstract class CosmosDbContext : IDisposable
{
    private CosmosClient? client;
    private IEventStore? eventStore;
    private string? streamId;

    public string Database { get; } = "EveneumDB";
    public abstract string Container { get; }
    public CosmosClient Client
    {
        get => this.client ?? throw new InvalidOperationException($"{nameof(Initialize)}() must be called first.");
        protected set => this.client = value;
    }
    public IEventStore EventStore
    {
        get => this.eventStore ?? throw new InvalidOperationException($"{nameof(Initialize)}() must be called first.");
        protected set => this.eventStore = value;
    }
    public EventStoreOptions EventStoreOptions { get; } = new EventStoreOptions() { QueryMaxItemCount = 100 };
    public virtual BulkDeleteMode BulkDeleteMode { get; } = Eveneum.BulkDeleteMode.StoredProcedure;

    public string StreamId
    {
        get => this.streamId ?? throw new InvalidOperationException($"{nameof(StreamId)} has not been set.");
        set => this.streamId = value;
    }
    public Stream? Stream { get; set; }
    public SampleMetadata? HeaderMetadata { get; set; }
    public SampleSnapshot Snapshot { get; set; }
    public SampleMetadata? SnapshotMetadata { get; set; }
    public SnapshotWriterSnapshot SnapshotWriterSnapshot { get; set; }
    public EventData[] NewEvents { get; set; } = [];
    public List<EventData> LoadAllEvents { get; set; } = [];
    public List<StreamHeader> LoadAllStreamHeaders { get; set; } = [];
    public EventData ReplacedEvent { get; set; }
    public List<IEveneumDocument> ExistingDocuments { get; set; } = [];
    public Response? Response { get; set; }
    public Exception? Exception { get; set; }

    public virtual void Dispose()
    {
        this.client?.Dispose();
    }

    public abstract bool AreEqual(object? expected, object? actual);

    public abstract Task Initialize();

    protected async Task DeleteAllDocuments<T>()
        where T : class, IEveneumDocument
    {
        var container = this.Client.GetDatabase(this.Database).GetContainer(this.Container);

        using var query = container.GetItemQueryIterator<T>("SELECT c.id, c.StreamId FROM c");

        var requestOptions = new ItemRequestOptions
        {
            ConsistencyLevel = ConsistencyLevel.Session
        };

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();

            var deleteTasks = response.Select(item => DeleteWithRetryAsync(
                container,
                item.Id,
                new PartitionKey(item.StreamId),
                requestOptions));

            await Task.WhenAll(deleteTasks);
        }
    }

    private static async Task DeleteWithRetryAsync(Container container, string id, PartitionKey partitionKey, ItemRequestOptions requestOptions, int maxRetries = 3)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                await container.DeleteItemAsync<dynamic>(id, partitionKey, requestOptions);
                return;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Already deleted, no action needed
                return;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.RequestTimeout && retryCount < maxRetries)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                await Task.Delay(delay);
            }
        }
    }
}
