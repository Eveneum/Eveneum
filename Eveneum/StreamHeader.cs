namespace Eveneum
{
    public struct StreamHeader
    {
        public string StreamId;
        public ulong Version;
        public object Metadata;
        public bool Deleted;

        internal StreamHeader(string streamId, ulong version, object metadata, bool deleted = false)
        {
            this.StreamId = streamId;
            this.Version = version;
            this.Metadata = metadata;
            this.Deleted = deleted;
        }
    }
}
