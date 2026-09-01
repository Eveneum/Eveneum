using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eveneum.Documents;

namespace Eveneum.Serialization
{
    /// <summary>
    /// Converter for IEveneumDocument interface to handle deserialization to concrete EveneumDocument type.
    /// System.Text.Json cannot deserialize to interface types, so this converter deserializes to the concrete type.
    /// </summary>
    public class IEveneumDocumentConverter : JsonConverter<IEveneumDocument>
    {
        public override IEveneumDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialize to the concrete EveneumDocument type
            return JsonSerializer.Deserialize<EveneumDocument>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, IEveneumDocument value, JsonSerializerOptions options)
        {
            // Serialize using the actual runtime type
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
