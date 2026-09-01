using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Eveneum.Serialization
{
    /// <summary>
    /// Converter for object properties that may contain JsonNode values.
    /// This ensures JsonNode is properly serialized to raw JSON rather than as an object graph.
    /// </summary>
    public class JsonNodeObjectConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonNode.Parse(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value is JsonNode jsonNode)
            {
                jsonNode.WriteTo(writer, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
