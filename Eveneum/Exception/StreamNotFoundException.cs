using System;

namespace Eveneum
{
    [Serializable]
    public class StreamNotFoundException : EveneumException
    {
        public StreamNotFoundException(string streamId) : base($"Stream '{streamId}' wasn't found")
        {
            this.StreamId = streamId;
        }

        protected StreamNotFoundException(string streamId, string message) : base(message)
        {
            this.StreamId = streamId;
        }

        public string StreamId
        {
            get { return (string)this.Data[nameof(StreamId)]; }
            protected set { this.Data[nameof(StreamId)] = value; }
        }

        protected StreamNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
