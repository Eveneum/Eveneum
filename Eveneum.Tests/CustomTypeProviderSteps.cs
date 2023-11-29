using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastructure;
using Eveneum.Serialization;
using System;
using Eveneum.Documents;
using Eveneum.Snapshots;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Eveneum.Tests
{
    public class CustomTypeProvider : ITypeProvider
    {
        public string GetIdentifierForType(Type type) => type.FullName;

        public Type GetTypeForIdentifier(string identifier) => Type.GetType(identifier);
    }

	[Binding]
	[Scope(Feature = "Custom Type provider")]
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

		[Then(@"the Snapshot Writer snapshot for version (\d+) is persisted")]
		public async Task ThenTheSnapshotWriterSnapshotForVersionIsPersisted(ulong version)
		{
			var streamId = this.Context.StreamId;
			var snapshot = new SnapshotWriterSnapshot(typeof(CustomSnapshotWriter).AssemblyQualifiedName);

			var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);

			Assert.IsNotEmpty(snapshotDocuments);

			var snapshotDocument = snapshotDocuments.Find(x => x.Version == version);
			Assert.IsNotNull(snapshotDocument);

			Assert.AreEqual(DocumentType.Snapshot, snapshotDocument.DocumentType);
			Assert.AreEqual(streamId, snapshotDocument.StreamId);
			Assert.AreEqual(version, snapshotDocument.Version);
			Assert.AreEqual(version + EveneumDocument.GetOrderingFraction(DocumentType.Snapshot), snapshotDocument.SortOrder);

			Assert.IsNull(snapshotDocument.MetadataType);
			Assert.IsFalse(snapshotDocument.Metadata.HasValues);

			var typeProvider = this.Context.EventStoreOptions.TypeProvider as CustomTypeProvider;

			Assert.AreEqual(typeProvider.GetIdentifierForType(typeof(SnapshotWriterSnapshot)), snapshotDocument.BodyType);
			Assert.AreEqual(JToken.FromObject(snapshot), snapshotDocument.Body);
			Assert.False(snapshotDocument.Deleted);
			Assert.IsNotNull(snapshotDocument.ETag);
		}
	}
}
