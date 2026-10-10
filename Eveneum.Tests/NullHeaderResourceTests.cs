using Eveneum.Documents;
using Eveneum.Persistence;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eveneum.Tests;

public class NullHeaderResourcePersistence(double requestCharge) : ICosmosPersistence
{
    public Task Initialize(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public ICosmosFeedIterator<IEveneumDocument> GetItemQueryIterator(string queryText, string partitionKey, int? maxItemCount = null) => throw new NotSupportedException();

    public ICosmosFeedIterator<IEveneumDocument> GetItemQueryIterator(QueryDefinition queryDefinition, int? maxItemCount = null) => throw new NotSupportedException();

    public Task<CosmosItemResponse<IEveneumDocument?>> ReadItemAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
        => Task.FromResult(new CosmosItemResponse<IEveneumDocument?>(null, requestCharge));

    public TransactionalBatch CreateTransactionalBatch(string partitionKey) => throw new NotSupportedException();

    public Task<CosmosItemResponse<IEveneumDocument?>> UpsertItemAsync(IEveneumDocument document, string partitionKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<CosmosItemResponse<IEveneumDocument?>> ReplaceItemAsync(IEveneumDocument document, string id, string partitionKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<DeleteResponse> DeleteItems(string streamId, string query, bool softDelete, int? ttl, byte batchSize, int? maxItemCount = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<StoredProcedureExecuteResponse<T>> ExecuteStoredProcedureAsync<T>(string storedProcedureId, string partitionKey, object?[] parameters, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task CreateOrUpdateStoredProcedureAsync(string procedureId, string procedureFileName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

[TestFixture]
public class NullHeaderResourceTests
{
    private const string StreamId = "S";
    private const double RequestCharge = 2.5;

    private static EventStore CreateEventStore() => new(new NullHeaderResourcePersistence(RequestCharge));

    [Test]
    public void WriteToStream_WithExpectedVersionAndNullHeaderResource_ThrowsStreamNotFoundException()
    {
        var eventStore = CreateEventStore();

        AssertStreamNotFound(() => eventStore.WriteToStream(StreamId, [], 10));
    }

    [Test]
    public void ReadHeader_WithNullHeaderResource_ThrowsStreamNotFoundException()
    {
        var eventStore = CreateEventStore();

        AssertStreamNotFound(() => eventStore.ReadHeader(StreamId));
    }

    [Test]
    public void DeleteStream_WithNullHeaderResource_ThrowsStreamNotFoundException()
    {
        var eventStore = CreateEventStore();

        AssertStreamNotFound(() => eventStore.DeleteStream(StreamId, 10));
    }

    [Test]
    public void CreateSnapshot_WithNullHeaderResource_ThrowsStreamNotFoundException()
    {
        var eventStore = CreateEventStore();

        AssertStreamNotFound(() => eventStore.CreateSnapshot(StreamId, 5, new SampleSnapshot { Version = 5 }));
    }

    [Test]
    public void DeleteSnapshots_WithNullHeaderResource_ThrowsStreamNotFoundException()
    {
        var eventStore = CreateEventStore();

        AssertStreamNotFound(() => eventStore.DeleteSnapshots(StreamId, 10));
    }

    [Test]
    public void DeleteEvent_WithNullHeaderResource_ThrowsStreamNotFoundException()
    {
        var eventStore = CreateEventStore();

        AssertStreamNotFound(() => eventStore.DeleteEvent(StreamId, 5));
    }

    private static void AssertStreamNotFound(Func<Task> action)
    {
        var exception = Assert.ThrowsAsync<StreamNotFoundException>(action);

        Assert.Multiple(() =>
        {
            Assert.That(exception.StreamId, Is.EqualTo(StreamId));
            Assert.That(exception.RequestCharge, Is.EqualTo(RequestCharge));
        });
    }
}
