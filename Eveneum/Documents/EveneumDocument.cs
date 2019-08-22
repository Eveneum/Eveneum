using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Eveneum.Documents
{
    enum DocumentType { Header = 1, Event, Snapshot }

    class EveneumDocument
    {
        public EveneumDocument(DocumentType documentType)
        {
            this.DocumentType = documentType;
        }

        public const char Separator = '~';


        [JsonProperty(PropertyName = "id")]
        public virtual string Id => this.GenerateId();

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

        internal string GenerateId()
        {
            switch(this.DocumentType)
            {
                case DocumentType.Header:
                    return this.StreamId;
                case DocumentType.Event:
                    return GenerateEventId(this.StreamId, this.Version);
                case DocumentType.Snapshot:
                    return $"{this.StreamId}{Separator}{this.Version}{Separator}S";
                default:
                    throw new NotSupportedException($"Document type '{this.DocumentType}' is not supported.");
            }
        }

        public static string GenerateEventId(string streamId, ulong version) => $"{streamId}{Separator}{version}";

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
