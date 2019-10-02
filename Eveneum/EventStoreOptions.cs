using System.Text.Json;
using Eveneum.Serialization;

namespace Eveneum
{
    public class EventStoreOptions
    {
        public DeleteMode DeleteMode { get; set; } = DeleteMode.SoftDelete;
        public JsonSerializerOptions JsonSerializerOptions { get; set; }
        public ITypeProvider TypeProvider { get; set; } = new PlatformTypeProvider();
    }
}
