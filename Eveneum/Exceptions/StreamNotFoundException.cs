using System;

namespace Eveneum
{
    [Serializable]
    public class StreamNotFoundException : EveneumException
    {
        public StreamNotFoundException(string streamId, double requestCharge) 
            : base(streamId, requestCharge, $"Stream '{streamId}' wasn't found")
        {}

        protected StreamNotFoundException(string streamId, double requestCharge, string message)
            : base(streamId, requestCharge, message)
        {}

        protected StreamNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
