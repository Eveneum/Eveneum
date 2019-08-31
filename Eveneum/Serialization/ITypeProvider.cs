using System;

namespace Eveneum.Serialization
{
    public interface ITypeProvider
    {
        string GetIdentifierForType(Type type);
        Type GetTypeForIdentifier(string identifier);
    }
}
