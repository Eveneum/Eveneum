using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Eveneum.Documents
{
    public enum DocumentType { Header = 1, Event, Snapshot }

    public class EveneumDocument
    {
        public EveneumDocument(string id, DocumentType documentType)
        {
            this.Id = id;
            this.DocumentType = documentType;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = nameof(DocumentType))]
        public DocumentType DocumentType { get; }

        [JsonProperty(PropertyName = nameof(StreamId))]
        public string StreamId { get; set; }

        [JsonProperty(PropertyName = nameof(Version))]
        public ulong Version { get; set; }

        [JsonProperty(PropertyName = nameof(MetadataType))]
        public string MetadataType { get; set; }

        [JsonProperty(PropertyName = nameof(Metadata))]
        public JToken Metadata { get; set; }

        [JsonProperty(PropertyName = nameof(BodyType))]
        public string BodyType { get; set; }

        [JsonProperty(PropertyName = nameof(Body))]
        public JToken Body { get; set; }

        [JsonProperty(PropertyName = nameof(SortOrder))]
        public decimal SortOrder => this.Version + GetOrderingFraction(this.DocumentType);

        [JsonProperty(PropertyName = nameof(Deleted))]
        public bool Deleted { get; set; }

        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "_ts")]
        public string Timestamp { get; set; }

        [JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeToLive { get; set; }

        internal static decimal GetOrderingFraction(DocumentType documentType)
        {
            switch(documentType)
            {
                case DocumentType.Header:
                    return 0.3M;
                case DocumentType.Snapshot:
                    return 0.2M;
                case DocumentType.Event:
                    return 0.1M;
                default:
                    throw new NotSupportedException($"Document type '{documentType}' is not supported.");
            }
        }
    }
}
