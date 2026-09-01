using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Eveneum.Documents;

namespace Eveneum.Serialization
{
    public class SystemTextJsonSerializer(JsonSerializerOptions options = null) : IJsonSerializer
    {
        private readonly JsonSerializerOptions Options = options ?? new JsonSerializerOptions();

        public object Serialize(object value)
        {
            if (value == null)
                return null;

            return JsonSerializer.SerializeToNode(value, Options);
        }

        public object Deserialize(object token, Type targetType)
        {
            if (token == null || targetType == null)
                return null;

            if (token is JsonNode jsonNode)
            {
                return jsonNode.Deserialize(targetType, Options);
            }

            throw new ArgumentException($"Token must be of type JsonNode, but was {token.GetType()}", nameof(token));
        }

        public IEveneumDocument CreateDocument(string id, DocumentType documentType)
        {
            return new EveneumDocument(id, documentType);
        }
    }
}
