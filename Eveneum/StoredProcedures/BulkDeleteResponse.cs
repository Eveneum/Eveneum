using System.Text.Json.Serialization;

namespace Eveneum.StoredProcedures
{
    public class BulkDeleteResponse
    {
        [JsonPropertyName("deleted")]
        public uint Deleted { get; set; }
        
        [JsonPropertyName("continuation")]
        public bool Continuation { get; set; }
    }
}
