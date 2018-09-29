namespace Eveneum.Documents
{
    class SnapshotDocument : EveneumDocument
    {
        public override string Id => $"{this.StreamId}{Separator}{this.Version}{Separator}S";
        public override DocumentType DocumentType => DocumentType.Snapshot;
    }
}
