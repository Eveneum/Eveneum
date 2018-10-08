using Newtonsoft.Json.Linq;

namespace Eveneum.Documents
{
    class SnapshotDocument : EveneumDocument
    {
        public override string Id => $"{this.StreamId}{Separator}{this.Version}{Separator}S";
        public override DocumentType DocumentType => DocumentType.Snapshot;

        public string MetadataType { get; set; }
        public JToken Metadata { get; set; }
        public string BodyType { get; set; }
        public JToken Body { get; set; }
    }
}
