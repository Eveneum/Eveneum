using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using NUnit.Framework;

namespace Eveneum.Tests.Infrastrature
{
    /// <summary>
    /// Base class for integration tests that use CosmosDB. Each test will execute against a new Collection.
    /// </summary>
    [TestFixture]
    public class CosmosTest
    {
        protected string Database { get; private set; }
        protected string Collection { get; private set; }

        [SetUp]
        public void SetUp()
        {
            this.Database = "EveneumDB";
            this.Collection = Guid.NewGuid().ToString();
        }

        [TearDown]
        public async Task TearDown()
        {
            await CosmosSetup.GetClient().DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection));
        }
    }
}
