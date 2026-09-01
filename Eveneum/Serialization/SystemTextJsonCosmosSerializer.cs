using System.IO;
using System.Linq;
using System.Text.Json;

namespace Eveneum.Serialization
{
    public class SystemTextJsonCosmosSerializer : Microsoft.Azure.Cosmos.CosmosSerializer
    {
        private readonly JsonSerializerOptions Options;

        public SystemTextJsonCosmosSerializer(JsonSerializerOptions options = null)
        {
            this.Options = options ?? new JsonSerializerOptions();

            if (!this.Options.Converters.OfType<IEveneumDocumentConverter>().Any())
                this.Options.Converters.Add(new IEveneumDocumentConverter());
        }

        public override T FromStream<T>(System.IO.Stream stream)
        {
            using (stream)
            {
                if (typeof(System.IO.Stream).IsAssignableFrom(typeof(T)))
                    return (T)(object)stream;

                return JsonSerializer.Deserialize<T>(stream, Options);
            }
        }

        public override System.IO.Stream ToStream<T>(T input)
        {
            var stream = new MemoryStream();
            
            // Get the actual runtime type to ensure we serialize the concrete type with all its attributes
            var actualType = input?.GetType() ?? typeof(T);
            
            // Serialize using the actual runtime type, not the generic parameter type
            // This ensures that if T is IEveneumDocument, we still serialize the concrete EveneumDocument
            JsonSerializer.Serialize(stream, input, actualType, Options);
            stream.Position = 0;
            return stream;
        }
    }
}
