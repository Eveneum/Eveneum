using System;

namespace Eveneum
{
    [Serializable]
    public class JsonDeserializationException : Exception
    {
        public JsonDeserializationException(string type, string json, Exception innerException) 
            : base($"Failed to deserialize an instance of '{type}'", innerException)
        {
            this.Type = type;
            this.Json = json;
        }

        public string Type
        {
            get { return (string)this.Data[nameof(Type)]; }
            private set { this.Data[nameof(Type)] = value; }
        }

        public string Json
        {
            get { return (string)this.Data[nameof(Json)]; }
            private set { this.Data[nameof(Json)] = value; }
        }

        protected JsonDeserializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
