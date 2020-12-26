using System;

namespace Eveneum
{
    [Serializable]
    public class StreamDeletedException : StreamNotFoundException
    {
        public StreamDeletedException(string streamId, double requestCharge)
            : base(streamId, requestCharge, $"Stream '{streamId}' has been deleted", null)
        {}

        protected StreamDeletedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
