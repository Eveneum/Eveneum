namespace Eveneum
{
    public struct Snapshot
    {
        internal Snapshot(object data, object metadata, ulong version)
        {
            this.Data = data;
            this.Metadata = metadata;
            this.Version = version;
        }

        public object Data;
        public object Metadata;
        public ulong Version;
    }
}
