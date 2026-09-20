using System;

namespace Eveneum
{
    [Serializable]
    public class SnapshotWriterNotFoundException : EveneumException
    {
        public SnapshotWriterNotFoundException(string streamId, double requestCharge, string snapshotWriterType)
            : base(streamId, requestCharge, $"Stream '{streamId}' contains a snapshot created by a Snapshot Writer ({snapshotWriterType}) but no Snapshot Writer is configured.")
        {
            this.SnapshotWriterType = snapshotWriterType;
        }

        public string SnapshotWriterType
        {
            get { return (string)this.Data[nameof(SnapshotWriterType)]; }
            private set { this.Data[nameof(SnapshotWriterType)] = value; }
        }

        protected SnapshotWriterNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
