namespace Eveneum
{
    public struct Stream
    {
        internal Stream(string streamId, ulong version, object metadata, object[] events, Snapshot? snapshot = null)
        {
            this.StreamId = streamId;
            this.Version = version;
            this.Metadata = metadata;
            this.Events = events;
            this.Snapshot = snapshot;
        }

        public string StreamId;
        public ulong Version;
        public object Metadata;
        public object[] Events;
        public Snapshot? Snapshot;
    }
}
