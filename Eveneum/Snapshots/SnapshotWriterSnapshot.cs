namespace Eveneum.Snapshots
{
    public struct SnapshotWriterSnapshot
    {
        public string SnapshotWriterType;

        internal SnapshotWriterSnapshot(string snapshotWriterType)
        {
            this.SnapshotWriterType = snapshotWriterType;
        }
    }
}
