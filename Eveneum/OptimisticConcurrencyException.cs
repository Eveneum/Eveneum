using System;

namespace Eveneum
{
    [Serializable]
    public class OptimisticConcurrencyException : Exception
    {
        public OptimisticConcurrencyException() { }
        public OptimisticConcurrencyException(string message) : base(message) { }
        public OptimisticConcurrencyException(string message, Exception inner) : base(message, inner) { }

        protected OptimisticConcurrencyException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
