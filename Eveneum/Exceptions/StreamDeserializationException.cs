using System;

namespace Eveneum
{
    [Serializable]
    public class StreamDeserializationException : EveneumException
    {
        public StreamDeserializationException(string streamId, double requestCharge, string type, Exception innerException)
            : base(streamId, requestCharge, $"Failed to deserialize stream '{streamId}'. Problematic type: '{type}'", innerException)
        {
            this.Type = type;
        }

        public string Type
        {
            get { return (string)this.Data[nameof(Type)]; }
            private set { this.Data[nameof(Type)] = value; }
        }

        protected StreamDeserializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
