namespace Eveneum.Documents
{
    class EventDocument : EveneumDocument
    {
        public override string Id => $"{this.StreamId}{Separator}{this.Version}";
        public override DocumentType DocumentType => DocumentType.Event;
    }
}
