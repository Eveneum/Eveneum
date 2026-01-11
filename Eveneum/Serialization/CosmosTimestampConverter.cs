using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eveneum.Serialization
{
    /// <summary>
    /// Converter for CosmosDB's _ts field which is returned as a number but we store as string.
    /// </summary>
    public class CosmosTimestampConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // CosmosDB returns _ts as a number (Unix timestamp)
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt64().ToString();
            }
            
            // But we might also store it as a string
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            // If null or undefined
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} for Timestamp field");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}
