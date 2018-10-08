namespace Eveneum
{
    public struct EventData
    {
        public ulong Version;
        public object Body;
        public object Metadata;

        public EventData(ulong version, object body, object metadata = null)
        {
            this.Version = version;
            this.Body = body;
            this.Metadata = metadata;
        }
    }
}
