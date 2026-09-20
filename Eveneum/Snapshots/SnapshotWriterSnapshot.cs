namespace Eveneum.Snapshots
{
    public struct SnapshotWriterSnapshot
    {
        public string SnapshotWriterType { get; set; }

        internal SnapshotWriterSnapshot(string snapshotWriterType)
        {
            this.SnapshotWriterType = snapshotWriterType;
        }
    }
}
