namespace Eveneum
{
    public struct EventData
    {
        public object Body;
        public object Metadata;
        public ulong Version;

        public EventData(object body, object metadata, ulong version)
        {
            this.Body = body;
            this.Metadata = metadata;
            this.Version = version;
        }
    }
}
