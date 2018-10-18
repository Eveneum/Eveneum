namespace Eveneum
{
    public struct Snapshot
    {
        internal Snapshot(object data, ulong version)
        {
            this.Data = data;
            this.Version = version;
        }

        public object Data;
        public ulong Version;
    }
}
