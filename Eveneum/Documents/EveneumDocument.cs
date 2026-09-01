using System;
using System.Text.Json.Serialization;
using Eveneum.Serialization;

namespace Eveneum.Documents
{
    public enum DocumentType { Header = 1, Event, Snapshot }

    public interface IEveneumDocument
    {
        string Id { get; set; }

        DocumentType DocumentType { get; }

        string StreamId { get; set; }

        ulong Version { get; set; }

        string MetadataType { get; set; }

        object Metadata { get; set; }

        string BodyType { get; set; }

        object Body { get; set; }

        decimal SortOrder { get; }

        bool Deleted { get; set; }

        string ETag { get; set; }

        string Timestamp { get; set; }

        int? TimeToLive { get; set; }
    }

    public class EveneumDocument(string id, DocumentType documentType) : IEveneumDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set;  } = id;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("DocumentType")]
        public DocumentType DocumentType { get; set; } = documentType;

        [JsonPropertyName("StreamId")]
        public string StreamId { get; set; }

        [JsonPropertyName("Version")]
        public ulong Version { get; set; }

        [JsonPropertyName("MetadataType")]
        public string MetadataType { get; set; }

        [JsonConverter(typeof(JsonNodeObjectConverter))]
        [JsonPropertyName("Metadata")]
        public object Metadata { get; set; }

        [JsonPropertyName("BodyType")]
        public string BodyType { get; set; }

        [JsonConverter(typeof(JsonNodeObjectConverter))]
        [JsonPropertyName("Body")]
        public object Body { get; set; }

        [JsonPropertyName("SortOrder")]
        public decimal SortOrder => this.Version + GetOrderingFraction(this.DocumentType);

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

        public static decimal GetOrderingFraction(DocumentType documentType) => documentType switch
        {
            DocumentType.Header => 0.3M,
            DocumentType.Snapshot => 0.2M,
            DocumentType.Event => 0.1M,
            _ => throw new NotSupportedException($"Document type '{documentType}' is not supported."),
        };
    }
}
