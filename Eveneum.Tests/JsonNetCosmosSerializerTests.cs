using Eveneum.Documents;
using Eveneum.NewtonsoftJson.Documents;
using Eveneum.NewtonsoftJson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace Eveneum.Tests
{
    public class CustomEveneumDocument(string id, DocumentType documentType) : IEveneumDocument
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = id;

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "DocumentType")]
        public DocumentType DocumentType { get; } = documentType;

        [JsonProperty(PropertyName = "StreamId")]
        public string StreamId { get; set; }

        [JsonProperty(PropertyName = "Version")]
        public ulong Version { get; set; }

        [JsonProperty(PropertyName = "MetadataType")]
        public string MetadataType { get; set; }

        [JsonProperty(PropertyName = "Metadata")]
        public object Metadata { get; set; }

        [JsonProperty(PropertyName = "BodyType")]
        public string BodyType { get; set; }

        [JsonProperty(PropertyName = "Body")]
        public object Body { get; set; }

        [JsonProperty(PropertyName = "SortOrder")]
        public decimal SortOrder => this.Version + EveneumDocument.GetOrderingFraction(this.DocumentType);

        [JsonProperty(PropertyName = "Deleted")]
        public bool Deleted { get; set; }

        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "_ts")]
        public string Timestamp { get; set; }

        [JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeToLive { get; set; }

        [JsonProperty(PropertyName = "CustomProperty")]
        public string CustomProperty { get; set; }
    }

    [TestFixture]
    public class JsonNetCosmosSerializerTests
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
                "_ts": "1700000000",
                "ttl": null,
                "CustomProperty": "custom-value"
            }
            """;

        private static System.IO.Stream CreateStream(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));

        [Test]
        public void FromStream_WithEveneumDocumentInterface_ReturnsDefaultDocument()
        {
            var serializer = new JsonNetCosmosSerializer(new JsonSerializerSettings());

            var document = serializer.FromStream<IEveneumDocument>(CreateStream(EventDocumentJson));

            Assert.That(document, Is.InstanceOf<NewtonsoftJsonEveneumDocument>());
            AssertDocument(document);
        }

        [Test]
        public void FromStream_WithDefaultDocumentType_ReturnsDefaultDocument()
        {
            var serializer = new JsonNetCosmosSerializer(new JsonSerializerSettings());

            var document = serializer.FromStream<NewtonsoftJsonEveneumDocument>(CreateStream(EventDocumentJson));

            AssertDocument(document);
        }

        [Test]
        public void FromStream_WithCustomDocumentType_ReturnsCustomDocument()
        {
            var serializer = new JsonNetCosmosSerializer(new JsonSerializerSettings());

            var document = serializer.FromStream<CustomEveneumDocument>(CreateStream(EventDocumentJson));

            Assert.That(document, Is.InstanceOf<CustomEveneumDocument>());
            AssertDocument(document);
            Assert.That(document.CustomProperty, Is.EqualTo("custom-value"));
        }

        [Test]
        public void FromStream_WithCustomDocumentType_RoundTrips()
        {
            var serializer = new JsonNetCosmosSerializer(new JsonSerializerSettings());

            var original = new CustomEveneumDocument("S~3", DocumentType.Event)
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
            var document = serializer.FromStream<CustomEveneumDocument>(stream);

            AssertDocument(document);
            Assert.That(document.CustomProperty, Is.EqualTo("custom-value"));
            Assert.That(document.Body, Is.InstanceOf<JToken>());
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
