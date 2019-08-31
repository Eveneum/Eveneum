using Newtonsoft.Json;
using Eveneum.Serialization;

namespace Eveneum
{
    public class EventStoreOptions
    {
        public DeleteMode DeleteMode { get; set; } = DeleteMode.SoftDelete;
        public JsonSerializer JsonSerializer { get; set; } = JsonSerializer.CreateDefault();
        public ITypeProvider TypeProvider { get; set; } = new PlatformTypeProvider();
    }
}
