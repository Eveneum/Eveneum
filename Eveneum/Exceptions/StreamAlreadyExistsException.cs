using System;

namespace Eveneum
{
    [Serializable]
    public class StreamAlreadyExistsException : EveneumException
    {
        public StreamAlreadyExistsException(string streamId, double requestCharge)
            : base(streamId, requestCharge, $"Stream '{streamId}' already exists.")
        {
        }

        protected StreamAlreadyExistsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
