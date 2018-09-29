namespace Eveneum
{
    public class Stream
    {
        internal Stream(string streamId, ulong version, object metadata, object[] events, Snapshot snapshot = null)
        {
            this.StreamId = streamId;
            this.Version = version;
            this.Metadata = metadata;
            this.Events = events;
            this.Snapshot = snapshot;
        }

        public string StreamId { get; }
        public ulong Version { get; }
        public object Metadata { get; }
        public object[] Events { get; }
        public Snapshot Snapshot { get; }

        public bool HasSnapshot => this.Snapshot != null;
    }
}
