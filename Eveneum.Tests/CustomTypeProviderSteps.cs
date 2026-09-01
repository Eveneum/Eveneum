using Eveneum.Documents;
using Eveneum.Serialization;
using Eveneum.Snapshots;
using Eveneum.Tests.Infrastructure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Eveneum.Tests
{
    public class CustomTypeProvider : ITypeProvider
    {
        public string GetIdentifierForType(Type type) => type.FullName;

        public Type GetTypeForIdentifier(string identifier) => Type.GetType(identifier);
    }

	[Binding]
	[Scope(Feature = "Custom Type provider")]
	public class CustomTypeProviderSteps(NewtonsoftCosmosDbContext newtonsoftContext, SystemTextJsonCosmosDbContext stjContext)
    {
        private readonly IReadOnlyCollection<CosmosDbContext> Contexts = [newtonsoftContext, stjContext];

        [Given(@"a custom Type Provider")]
		public void GivenACustomTypeProvider()
		{
			foreach (var context in this.Contexts)
			{
				context.EventStoreOptions.TypeProvider = new CustomTypeProvider();
			}
		}

		[Then(@"the Snapshot Writer snapshot for version (\d+) is persisted")]
		public async Task ThenTheSnapshotWriterSnapshotForVersionIsPersisted(ulong version)
		{
			var snapshot = new SnapshotWriterSnapshot(typeof(CustomSnapshotWriter).AssemblyQualifiedName);

            await Task.WhenAll(this.Contexts.Select(async context =>
            {
                var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(context.Client, context.Database, context.Container, context.StreamId, DocumentType.Snapshot);
                Assert.That(snapshotDocuments, Is.Not.Empty);

                var snapshotDocument = snapshotDocuments.Find(x => x.Version == version);
                Assert.That(snapshotDocument, Is.Not.Null);
                Assert.That(snapshotDocument.DocumentType, Is.EqualTo(DocumentType.Snapshot));
                Assert.That(snapshotDocument.StreamId, Is.EqualTo(context.StreamId));
                Assert.That(snapshotDocument.Version, Is.EqualTo(version));
                Assert.That(snapshotDocument.SortOrder, Is.EqualTo(version + EveneumDocument.GetOrderingFraction(DocumentType.Snapshot)));
                Assert.That(snapshotDocument.MetadataType, Is.Null);
                Assert.That(snapshotDocument.Metadata, Is.Null);

                var typeProvider = context.EventStoreOptions.TypeProvider as CustomTypeProvider;
                Assert.That(snapshotDocument.BodyType, Is.EqualTo(typeProvider.GetIdentifierForType(typeof(SnapshotWriterSnapshot))));
                Assert.That(context.AreEqual(snapshotDocument.Body, snapshot), Is.True);
                Assert.That(snapshotDocument.Deleted, Is.False);
                Assert.That(snapshotDocument.ETag, Is.Not.Null);
            }));
		}
	}
}
