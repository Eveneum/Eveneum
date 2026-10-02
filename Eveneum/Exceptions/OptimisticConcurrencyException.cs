using System;

namespace Eveneum;

[Serializable]
public class OptimisticConcurrencyException : EveneumException
{
    public OptimisticConcurrencyException(string streamId, double requestCharge, ulong expectedVersion, ulong actualVersion)
        : base(streamId, requestCharge, $"Expected stream '{streamId}' to have version {expectedVersion} but was {actualVersion}.")
    {
        this.ExpectedVersion = expectedVersion;
        this.ActualVersion = actualVersion;
    }

    public ulong ExpectedVersion
    {
        get { return this.Data[nameof(ExpectedVersion)] is ulong expectedVersion ? expectedVersion : default; }
        private set { this.Data[nameof(ExpectedVersion)] = value; }
    }

    public ulong ActualVersion
    {
        get { return this.Data[nameof(ActualVersion)] is ulong actualVersion ? actualVersion : default; }
        private set { this.Data[nameof(ActualVersion)] = value; }
    }

    protected OptimisticConcurrencyException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
