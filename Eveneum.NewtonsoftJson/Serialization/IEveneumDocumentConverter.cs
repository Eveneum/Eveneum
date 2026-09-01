using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Eveneum.Documents;

namespace Eveneum.NewtonsoftJson.Serialization
{
    public class IEveneumDocumentConverter : JsonConverter<IEveneumDocument>
    {
        public override IEveneumDocument ReadJson(JsonReader reader, Type objectType, IEveneumDocument existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var document = new Documents.NewtonsoftJsonEveneumDocument(
                jObject["id"]?.Value<string>(),
                jObject["DocumentType"]?.ToObject<DocumentType>() ?? DocumentType.Header
            );

            serializer.Populate(jObject.CreateReader(), document);
            return document;
        }

        public override void WriteJson(JsonWriter writer, IEveneumDocument value, JsonSerializer serializer)
        {
            // Get the concrete type and serialize it without this converter to avoid circular reference
            var concreteType = value.GetType();
            var token = JToken.FromObject(value, GetSerializerWithoutThisConverter(serializer));
            token.WriteTo(writer);
        }

        private static JsonSerializer GetSerializerWithoutThisConverter(JsonSerializer original)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = original.ContractResolver,
                Formatting = original.Formatting,
                DateFormatHandling = original.DateFormatHandling,
                DateTimeZoneHandling = original.DateTimeZoneHandling,
                NullValueHandling = original.NullValueHandling,
                DefaultValueHandling = original.DefaultValueHandling,
                ReferenceLoopHandling = original.ReferenceLoopHandling,
                TypeNameHandling = original.TypeNameHandling
            };

            // Copy all converters except this one
            foreach (var converter in original.Converters)
            {
                if (!(converter is IEveneumDocumentConverter))
                {
                    settings.Converters.Add(converter);
                }
            }

            return JsonSerializer.Create(settings);
        }
    }
}
