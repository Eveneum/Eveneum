using System;

namespace Eveneum
{
    [Serializable]
    public class StreamNotFoundException : EveneumException
    {
        public StreamNotFoundException(string streamId, double requestCharge)
            : this(streamId, requestCharge, null)
        { }

        public StreamNotFoundException(string streamId, double requestCharge, Exception inner)
            : this(streamId, requestCharge, $"Stream '{streamId}' wasn't found", inner)
        { }

        protected StreamNotFoundException(string streamId, double requestCharge, string message, Exception inner)
            : base(streamId, requestCharge, message, inner)
        {}

        protected StreamNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
