namespace Eveneum
{
    public struct EventData
    {
        public string StreamId;
        public object Body;
        public object Metadata;
        public ulong Version;

        public EventData(string streamId, object body, object metadata, ulong version)
        {
            this.StreamId = streamId;
            this.Body = body;
            this.Metadata = metadata;
            this.Version = version;
        }
    }
}
