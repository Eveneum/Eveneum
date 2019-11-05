using System;

namespace Eveneum
{
    [Serializable]
    public abstract class EveneumException : Exception
    {
        public EveneumException() { }
        public EveneumException(string message) : base(message) { }
        public EveneumException(string message, Exception inner) : base(message, inner) { }

        protected EveneumException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
