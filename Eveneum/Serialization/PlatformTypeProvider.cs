using System;
using System.Collections.Concurrent;

namespace Eveneum.Serialization
{
    class PlatformTypeProvider : ITypeProvider
    {
        private readonly ConcurrentDictionary<string, Type> Cache = new ConcurrentDictionary<string, Type>();

        public string GetIdentifierForType(Type type) => type.AssemblyQualifiedName;

        public Type GetTypeForIdentifier(string identifier) => this.Cache.GetOrAdd(identifier, t => Type.GetType(t));
    }
}
