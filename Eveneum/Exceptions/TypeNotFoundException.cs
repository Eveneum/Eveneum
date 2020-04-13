using System;

namespace Eveneum
{
    [Serializable]
    public class TypeNotFoundException : Exception
    {
        public TypeNotFoundException(string type) : base($"Type '{type}' wasn't found")
        {
            this.Type = type;
        }

        public string Type
        {
            get { return (string)this.Data[nameof(Type)]; }
            private set { this.Data[nameof(Type)] = value; }
        }

        protected TypeNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
