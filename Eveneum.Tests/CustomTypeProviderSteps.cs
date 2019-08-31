using System.Threading.Tasks;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastrature;
using Eveneum.Serialization;
using System;
using Eveneum.Documents;
using NUnit.Framework;

namespace Eveneum.Tests
{
    public class CustomTypeProvider : ITypeProvider
    {
        public string GetIdentifierForType(Type type) => type.FullName;

        public Type GetTypeForIdentifier(string identifier) => Type.GetType(identifier);
    }

    [Binding]
    public class CustomTypeProviderSteps
    {
        private readonly CosmosDbContext Context;

        CustomTypeProviderSteps(CosmosDbContext context)
        {
            this.Context = context;
        }

        [Given(@"a custom Type Provider")]
        public void GivenACustomTypeProvider()
        {
            this.Context.EventStoreOptions.TypeProvider = new CustomTypeProvider();
        }
    }
}
