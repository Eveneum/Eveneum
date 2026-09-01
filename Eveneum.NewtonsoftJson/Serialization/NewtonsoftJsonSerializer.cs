using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Eveneum.Serialization;
using Eveneum.Documents;
using Eveneum.NewtonsoftJson.Documents;

namespace Eveneum.NewtonsoftJson.Serialization
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializer _serializer;

        public NewtonsoftJsonSerializer(JsonSerializer serializer = null)
        {
            _serializer = serializer ?? JsonSerializer.CreateDefault();
        }

        public NewtonsoftJsonSerializer(JsonSerializerSettings settings)
            : this(JsonSerializer.Create(settings))
        {
        }

        public object Serialize(object value)
        {
            if (value == null)
                return null;

            return JToken.FromObject(value, _serializer);
        }

        public object Deserialize(object token, Type targetType)
        {
            if (token == null || targetType == null)
                return null;

            if (token is JToken jToken)
            {
                return jToken.ToObject(targetType, _serializer);
            }

            throw new ArgumentException($"Token must be of type JToken, but was {token.GetType()}", nameof(token));
        }

        public IEveneumDocument CreateDocument(string id, DocumentType documentType)
        {
            return new NewtonsoftJsonEveneumDocument(id, documentType);
        }
    }
}
