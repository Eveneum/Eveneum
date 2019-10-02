using System.IO;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Eveneum
{
    class CosmosJsonSerializer : CosmosSerializer
    {
        public CosmosJsonSerializer(JsonSerializerOptions jsonSerializerOptions)
        {
            this.JsonSerializerOptions = jsonSerializerOptions;
        }

        public JsonSerializerOptions JsonSerializerOptions { get; }

        public override T FromStream<T>(System.IO.Stream stream)
        {
            return JsonSerializer.DeserializeAsync<T>(stream, this.JsonSerializerOptions).Result;
        }

        public override System.IO.Stream ToStream<T>(T input)
        {
            var stream = new MemoryStream();
            JsonSerializer.SerializeAsync<T>(stream, input, this.JsonSerializerOptions).Wait();
            stream.Position = 0;

            return stream;
        }
    }
}
