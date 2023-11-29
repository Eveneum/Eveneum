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

			Assert.That(snapshotDocuments, Is.Not.Empty);

			var snapshotDocument = snapshotDocuments.Find(x => x.Version == version);
			Assert.That(snapshotDocument, Is.Not.Null);

			Assert.That(snapshotDocument.DocumentType, Is.EqualTo(DocumentType.Snapshot));
			Assert.That(snapshotDocument.StreamId, Is.EqualTo(streamId));
			Assert.That(snapshotDocument.Version, Is.EqualTo(version));
			Assert.That(snapshotDocument.SortOrder, Is.EqualTo(version + EveneumDocument.GetOrderingFraction(DocumentType.Snapshot)));

			Assert.That(snapshotDocument.MetadataType, Is.Null);
			Assert.That(snapshotDocument.Metadata.HasValues, Is.False);

			var typeProvider = this.Context.EventStoreOptions.TypeProvider as CustomTypeProvider;

			Assert.That(snapshotDocument.BodyType, Is.EqualTo(typeProvider.GetIdentifierForType(typeof(SnapshotWriterSnapshot))));
			Assert.That(snapshotDocument.Body, Is.EqualTo(JToken.FromObject(snapshot)));
			Assert.That(snapshotDocument.Deleted, Is.False);
			Assert.That(snapshotDocument.ETag, Is.Not.Null);
		}
	}
}
