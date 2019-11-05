using System;

namespace Eveneum
{
    [Serializable]
    public class OptimisticConcurrencyException : EveneumException
    {
        public OptimisticConcurrencyException(string streamId, ulong expectedVersion, ulong actualVersion)
            : base($"Expected stream '{streamId}' to have version {expectedVersion} but was {actualVersion}.")
        {
            this.StreamId = streamId;
            this.ExpectedVersion = expectedVersion;
            this.ActualVersion = actualVersion;
        }

        public string StreamId
        {
            get { return (string)this.Data[nameof(StreamId)]; }
            private set { this.Data[nameof(StreamId)] = value; }
        }

        public ulong ExpectedVersion
        {
            get { return (ulong)this.Data[nameof(ExpectedVersion)]; }
            private set { this.Data[nameof(ExpectedVersion)] = value; }
        }

        public ulong ActualVersion
        {
            get { return (ulong)this.Data[nameof(ActualVersion)]; }
            private set { this.Data[nameof(ActualVersion)] = value; }
        }

        protected OptimisticConcurrencyException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
