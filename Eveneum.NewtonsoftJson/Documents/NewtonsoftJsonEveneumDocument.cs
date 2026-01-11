using Eveneum.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Eveneum.NewtonsoftJson.Documents
{
    public class NewtonsoftJsonEveneumDocument(string id, DocumentType documentType) : IEveneumDocument
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
    }
}
