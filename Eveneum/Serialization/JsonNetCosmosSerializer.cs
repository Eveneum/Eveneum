using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Eveneum.Serialization
{
    public class JsonNetCosmosSerializer : Microsoft.Azure.Cosmos.CosmosSerializer
    {
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
        private readonly JsonSerializer Serializer;

        public JsonNetCosmosSerializer(JsonSerializer serializer)
        {
            this.Serializer = serializer;
        }

        public JsonNetCosmosSerializer(JsonSerializerSettings serializerSettings)
            : this(JsonSerializer.Create(serializerSettings))
        {
        }

        public override T FromStream<T>(System.IO.Stream stream)
        {
            using (stream)
            {
                if (typeof(Stream).IsAssignableFrom(typeof(T)))
                    return (T)(object)stream;

                using var streamReader = new StreamReader(stream);
                using var textReader = new JsonTextReader(streamReader);
                
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
