using System.IO;
using System.Text;
using Newtonsoft.Json;
using Eveneum.Documents;
using Eveneum.NewtonsoftJson.Documents;

namespace Eveneum.NewtonsoftJson.Serialization
{
    public class JsonNetCosmosSerializer : Microsoft.Azure.Cosmos.CosmosSerializer
    {
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
        private readonly JsonSerializer Serializer;

        public JsonNetCosmosSerializer(JsonSerializer serializer)
        {
            this.Serializer = serializer;
            
            // Add converter for IEveneumDocument if not already present
            if (!HasConverter<IEveneumDocumentConverter>(serializer))
            {
                this.Serializer.Converters.Add(new IEveneumDocumentConverter());
            }
        }

        public JsonNetCosmosSerializer(JsonSerializerSettings serializerSettings)
            : this(CreateSerializerWithConverter(serializerSettings))
        {
        }

        private static JsonSerializer CreateSerializerWithConverter(JsonSerializerSettings serializerSettings)
        {
            var settings = serializerSettings ?? new JsonSerializerSettings();
            if (!HasConverter<IEveneumDocumentConverter>(settings.Converters))
            {
                settings.Converters.Add(new IEveneumDocumentConverter());
            }
            return JsonSerializer.Create(settings);
        }

        private static bool HasConverter<T>(JsonSerializer serializer) where T : JsonConverter
        {
            foreach (var converter in serializer.Converters)
            {
                if (converter is T)
                    return true;
            }
            return false;
        }

        private static bool HasConverter<T>(System.Collections.Generic.IList<JsonConverter> converters) where T : JsonConverter
        {
            foreach (var converter in converters)
            {
                if (converter is T)
                    return true;
            }
            return false;
        }

        public override T FromStream<T>(System.IO.Stream stream)
        {
            using (stream)
            {
                if (typeof(Stream).IsAssignableFrom(typeof(T)))
                    return (T)(object)stream;

                using var streamReader = new StreamReader(stream);
                using var textReader = new JsonTextReader(streamReader);
                
                // Handle EveneumDocument requests - deserialize to NewtonsoftJsonEveneumDocument
                if (typeof(T) == typeof(EveneumDocument) || typeof(IEveneumDocument).IsAssignableFrom(typeof(T)))
                {
                    var document = this.Serializer.Deserialize<NewtonsoftJsonEveneumDocument>(textReader);
                    return (T)(object)document;
                }
                
                return this.Serializer.Deserialize<T>(textReader);
            }
        }

        public override System.IO.Stream ToStream<T>(T input)
        {
            var stream = new MemoryStream();

            using var streamWriter = new StreamWriter(stream, encoding: JsonNetCosmosSerializer.DefaultEncoding, bufferSize: 1024, leaveOpen: true);
            using JsonWriter writer = new JsonTextWriter(streamWriter);

            this.Serializer.Serialize(writer, input);

            writer.Flush();
            streamWriter.Flush();

            stream.Position = 0;

            return stream;
        }
    }
}
