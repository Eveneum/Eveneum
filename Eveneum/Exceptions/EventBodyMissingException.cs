using System;

namespace Eveneum;

[Serializable]
public class EventBodyMissingException : EveneumException
{
    public EventBodyMissingException(string streamId, ulong version, double requestCharge)
        : base(streamId, requestCharge, $"Event version {version} of stream '{streamId}' has no Body.")
    {
        this.Version = version;
    }

    public ulong Version
    {
        get { return this.GetRequired<ulong>(nameof(Version)); }
        private set { this.Data[nameof(Version)] = value; }
    }

    protected EventBodyMissingException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
