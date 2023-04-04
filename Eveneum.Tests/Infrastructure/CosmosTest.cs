using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Eveneum.Tests.Infrastructure
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
            Database = "EveneumDB";
            Collection = Guid.NewGuid().ToString();
        }

        [TearDown]
        public async Task TearDown()
        {
            await CosmosSetup.GetClient().GetDatabase(Database).GetContainer(Collection).DeleteContainerAsync();
        }
    }
}
