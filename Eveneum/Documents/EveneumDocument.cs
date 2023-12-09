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
        public DocumentType DocumentType { get; }

        public string StreamId { get; set; }

        public ulong Version { get; set; }

        public string MetadataType { get; set; }
        public JToken Metadata { get; set; }
        public string BodyType { get; set; }
        public JToken Body { get; set; }

        public decimal SortOrder => this.Version + GetOrderingFraction(this.DocumentType);

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
