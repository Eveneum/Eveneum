namespace Eveneum
{
    public struct StreamHeader
    {
        internal StreamHeader(string streamId, ulong version, object metadata)
        {
            this.StreamId = streamId;
            this.Version = version;
            this.Metadata = metadata;
        }

        public string StreamId;
        public ulong Version;
        public object Metadata;
    }
}
