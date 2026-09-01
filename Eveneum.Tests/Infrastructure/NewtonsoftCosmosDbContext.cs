using Eveneum.NewtonsoftJson.Documents;
using Eveneum.NewtonsoftJson.Serialization;
using Eveneum.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using System.Threading.Tasks;

namespace Eveneum.Tests.Infrastructure
{
    public class NewtonsoftCosmosDbContext : CosmosDbContext
    {
        public override string Container { get; } = "Newtonsoft";

        public JsonSerializerSettings JsonSerializerSettings { get; set; } = new JsonSerializerSettings();

        public override async Task Initialize()
        {
            this.JsonSerializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            this.Client = await CosmosSetup.GetClientWithNewtonsoftJson(this.Database, this.Container, this.JsonSerializerSettings);

            await DeleteAllDocuments<NewtonsoftJsonEveneumDocument>();

            this.EventStoreOptions.JsonSerializer = new NewtonsoftJsonSerializer(this.JsonSerializerSettings);
            
            var persistence = new CosmosPersistence<NewtonsoftJsonEveneumDocument>(this.Client, this.Database, this.Container, this.EventStoreOptions.BulkDeleteMode);
            this.EventStore = new EventStore(persistence, this.EventStoreOptions);

            await this.EventStore.Initialize();
        }

        public override bool AreEqual(object first, object second)
        {
            if (first is null && second is null)
                return true;

            if (first is null != second is null)
                return false;

            var firstToken = first is JToken firstJToken ? firstJToken : (JToken)this.EventStoreOptions.JsonSerializer.Serialize(first);
            var secondToken = second is JToken secondJToken ? secondJToken : (JToken)this.EventStoreOptions.JsonSerializer.Serialize(second);

            return JToken.DeepEquals(firstToken, secondToken);
        }
    }
}
