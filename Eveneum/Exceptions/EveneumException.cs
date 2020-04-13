using System;

namespace Eveneum
{
    [Serializable]
    public abstract class EveneumException : Exception
    {
        public EveneumException(string streamId, double requestCharge, string message)
            : this(streamId, requestCharge, message, null) 
        { }

        public EveneumException(string streamId, double requestCharge, string message, Exception inner)
            : base(message, inner)
        {
            this.StreamId = streamId;
            this.RequestCharge = requestCharge;
        }

        public string StreamId
        {
            get { return (string)this.Data[nameof(StreamId)]; }
            private set { this.Data[nameof(StreamId)] = value; }
        }

        public double RequestCharge
        {
            get { return (double)this.Data[nameof(RequestCharge)]; }
            private set { this.Data[nameof(RequestCharge)] = value; }
        }

        protected EveneumException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
