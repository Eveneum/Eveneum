using System;

namespace Eveneum
{
    [Serializable]
    public class EventAlreadyExistsException : EveneumException
    {
        public EventAlreadyExistsException(string streamId, ulong version, double requestCharge)
            : base(streamId, requestCharge, $"Event version {version} already exists in stream '{streamId}'.")
        {
            this.Version = version;
        }

        public ulong Version
        {
            get { return (ulong)this.Data[nameof(Version)]; }
            private set { this.Data[nameof(Version)] = value; }
        }

        protected EventAlreadyExistsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
