using System;

namespace Eveneum
{
    [Serializable]
    public class StreamDeletedException : StreamNotFoundException
    {
        public StreamDeletedException(string streamId) : base(streamId, $"Stream '{streamId}' has been deleted")
        {
            this.StreamId = streamId;
        }

        protected StreamDeletedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
