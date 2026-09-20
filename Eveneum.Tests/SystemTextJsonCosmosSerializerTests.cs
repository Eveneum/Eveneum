using Eveneum.Documents;
using Eveneum.Serialization;
using NUnit.Framework;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eveneum.Tests
{
    public class CustomSystemTextJsonEveneumDocument(string id, DocumentType documentType) : IEveneumDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = id;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("DocumentType")]
        public DocumentType DocumentType { get; set; } = documentType;

        [JsonPropertyName("StreamId")]
        public string StreamId { get; set; }

        [JsonPropertyName("Version")]
        public ulong Version { get; set; }

        [JsonPropertyName("MetadataType")]
        public string MetadataType { get; set; }

        [JsonPropertyName("Metadata")]
        public object Metadata { get; set; }

        [JsonPropertyName("BodyType")]
        public string BodyType { get; set; }

        [JsonPropertyName("Body")]
        public object Body { get; set; }

        [JsonPropertyName("SortOrder")]
        public decimal SortOrder => this.Version + EveneumDocument.GetOrderingFraction(this.DocumentType);

        [JsonPropertyName("Deleted")]
        public bool Deleted { get; set; }

        [JsonPropertyName("_etag")]
        public string ETag { get; set; }

        [JsonConverter(typeof(CosmosTimestampConverter))]
        [JsonPropertyName("_ts")]
        public string Timestamp { get; set; }

        [JsonPropertyName("ttl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TimeToLive { get; set; }

        [JsonPropertyName("CustomProperty")]
        public string CustomProperty { get; set; }
    }

    [TestFixture]
    public class SystemTextJsonCosmosSerializerTests
    {
        private static readonly string EventDocumentJson = """
            {
                "id": "S~3",
                "DocumentType": "Event",
                "StreamId": "S",
                "Version": 3,
                "MetadataType": null,
                "Metadata": null,
                "BodyType": "Eveneum.Tests.SampleEvent, Eveneum.Tests",
                "Body": { "Version": 3, "Number": 1.25 },
                "SortOrder": 3.1,
                "Deleted": false,
                "_etag": "etag",
                "_ts": 1700000000,
                "ttl": null,
                "CustomProperty": "custom-value"
            }
            """;

        private static System.IO.Stream CreateStream(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));

        [Test]
        public void FromStream_WithEveneumDocumentInterface_ReturnsDefaultDocument()
        {
            var serializer = new SystemTextJsonCosmosSerializer();

            var document = serializer.FromStream<IEveneumDocument>(CreateStream(EventDocumentJson));

            Assert.That(document, Is.InstanceOf<EveneumDocument>());
            AssertDocument(document);
        }

        [Test]
        public void FromStream_WithDefaultDocumentType_ReturnsDefaultDocument()
        {
            var serializer = new SystemTextJsonCosmosSerializer();

            var document = serializer.FromStream<EveneumDocument>(CreateStream(EventDocumentJson));

            AssertDocument(document);
        }

        [Test]
        public void FromStream_WithCustomDocumentType_ReturnsCustomDocument()
        {
            var serializer = new SystemTextJsonCosmosSerializer();

            var document = serializer.FromStream<CustomSystemTextJsonEveneumDocument>(CreateStream(EventDocumentJson));

            Assert.That(document, Is.InstanceOf<CustomSystemTextJsonEveneumDocument>());
            AssertDocument(document);
            Assert.That(document.CustomProperty, Is.EqualTo("custom-value"));
        }

        [Test]
        public void FromStream_WithCustomDocumentType_RoundTrips()
        {
            var serializer = new SystemTextJsonCosmosSerializer();

            var original = new CustomSystemTextJsonEveneumDocument("S~3", DocumentType.Event)
            {
                StreamId = "S",
                Version = 3,
                BodyType = "Eveneum.Tests.SampleEvent, Eveneum.Tests",
                Body = new SampleEvent { Version = 3, Number = 1.25m },
                ETag = "etag",
                Timestamp = "1700000000",
                CustomProperty = "custom-value"
            };

            using var stream = serializer.ToStream(original);
            var document = serializer.FromStream<CustomSystemTextJsonEveneumDocument>(stream);

            AssertDocument(document);
            Assert.That(document.CustomProperty, Is.EqualTo("custom-value"));
            Assert.That(document.Body, Is.InstanceOf<JsonElement>());
        }

        private static void AssertDocument(IEveneumDocument document)
        {
            Assert.Multiple(() =>
            {
                Assert.That(document.Id, Is.EqualTo("S~3"));
                Assert.That(document.DocumentType, Is.EqualTo(DocumentType.Event));
                Assert.That(document.StreamId, Is.EqualTo("S"));
                Assert.That(document.Version, Is.EqualTo(3UL));
                Assert.That(document.MetadataType, Is.Null);
                Assert.That(document.Metadata, Is.Null);
                Assert.That(document.BodyType, Is.EqualTo("Eveneum.Tests.SampleEvent, Eveneum.Tests"));
                Assert.That(document.Deleted, Is.False);
                Assert.That(document.ETag, Is.EqualTo("etag"));
                Assert.That(document.Timestamp, Is.EqualTo("1700000000"));
                Assert.That(document.TimeToLive, Is.Null);
                Assert.That(document.SortOrder, Is.EqualTo(3.1M));
            });
        }
    }
}
