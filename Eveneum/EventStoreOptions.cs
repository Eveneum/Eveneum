using Newtonsoft.Json;
using Eveneum.Serialization;
using System;

namespace Eveneum
{
    public class EventStoreOptions
    {
        public DeleteMode DeleteMode { get; set; } = DeleteMode.SoftDelete;
        public byte BatchSize { get; set; } = 100;
        public int QueryMaxItemCount { get; set; } = 1000;
        public JsonSerializer JsonSerializer { get; set; } = JsonSerializer.CreateDefault();
        public ITypeProvider TypeProvider { get; set; } = new PlatformTypeProvider();
        public bool IgnoreMissingTypes { get; set; } = false;

        // calculate document TTL based on given timespan in case Delete mode is set to TtlDelete
        public TimeSpan StreamTimeToLiveAfterDelete { get; set; } = TimeSpan.FromHours(24);

    }
}
