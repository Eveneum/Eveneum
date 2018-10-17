namespace Eveneum
{
    public class Snapshot
    {
        public Snapshot(object data, ulong version)
        {
            this.Data = data;
            this.Version = version;
        }

        public object Data { get; }
        public ulong Version { get; }
    }
}
