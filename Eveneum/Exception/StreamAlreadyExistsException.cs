using System;

namespace Eveneum
{
    [Serializable]
    public class StreamAlreadyExistsException : Exception
    {
        public StreamAlreadyExistsException(string streamId) : base($"Stream '{streamId}' already exists.")
        {
            this.StreamId = streamId;
        }

        public string StreamId
        {
            get { return (string)this.Data[nameof(StreamId)]; }
            private set { this.Data[nameof(StreamId)] = value; }
        }

        protected StreamAlreadyExistsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
