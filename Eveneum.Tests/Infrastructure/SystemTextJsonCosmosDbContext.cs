using Eveneum.Documents;
using Eveneum.Persistence;
using Eveneum.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Eveneum.Tests.Infrastructure;

public class SystemTextJsonCosmosDbContext : CosmosDbContext
{
    public override string Container { get; } = "SystemTextJson";

    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions
    {
        IncludeFields = true
    };

    public override async Task Initialize()
    {
        this.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        this.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        this.Client = await CosmosSetup.GetClientWithSystemTextJson(this.Database, this.Container, this.JsonSerializerOptions);
        await DeleteAllDocuments<EveneumDocument>();

        this.EventStoreOptions.JsonSerializer = new SystemTextJsonSerializer(this.JsonSerializerOptions);
        
        var persistence = new CosmosPersistence<EveneumDocument>(this.Client, this.Database, this.Container, this.BulkDeleteMode);
        this.EventStore = new EventStore(persistence, this.EventStoreOptions);

        await this.EventStore.Initialize();
    }

    public override bool AreEqual(object? first, object? second)
    {
        if (first is null && second is null)
            return true;

        if (first is null != second is null)
            return false;

        var serializer = this.EventStoreOptions.JsonSerializer ?? throw new InvalidOperationException($"{nameof(Initialize)}() must be called first.");
        var firstToken = first is JsonNode firstNode ? firstNode : (JsonNode?)serializer.Serialize(first);
        var secondToken = second is JsonNode secondNode ? secondNode : (JsonNode?)serializer.Serialize(second);

        return JsonNode.DeepEquals(firstToken, secondToken);
    }
}

public class SystemTextJsonLinuxCosmosDbContext : SystemTextJsonCosmosDbContext
{
    public override string Container => base.Container + "Linux";
    public override BulkDeleteMode BulkDeleteMode => Eveneum.BulkDeleteMode.TransactionalBatch;
}
