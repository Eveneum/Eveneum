namespace Eveneum.Documents
{
    class HeaderDocument : EveneumDocument
    {
        public override string Id => this.StreamId;
        public override DocumentType DocumentType => DocumentType.Header;
    }
}
