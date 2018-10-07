using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Eveneum.Tests.Infrastrature
{
    class CosmosDbContext : IDisposable
    {
        public string Database { get; private set; }
        public string Collection { get; private set; }

        public DocumentClient Client { get; private set; }
        public IEventStore EventStore { get; private set; }
        public bool Partitioned { get; set; }
        public string Partition { get; set; }
        public PartitionKey PartitionKey => this.Partitioned ? new PartitionKey(this.Partition) : null;

        public CosmosDbContext()
        {
            this.Database = "EveneumDB";
            this.Collection = Guid.NewGuid().ToString();
        }

        internal async Task Initialize()
        {
            this.Client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: this.Partitioned);
            this.EventStore = new EventStore(this.Client, this.Database, this.Collection, this.Partition);
        }

        public void Dispose()
        {
            CosmosSetup.GetClient().DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection)).Wait();
        }
    }
}
