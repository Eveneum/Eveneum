using System;
using System.Collections.Concurrent;

namespace Eveneum.Serialization
{
    class PlatformTypeProvider : ITypeProvider
    {
        private readonly ConcurrentDictionary<string, Type> Cache = new ConcurrentDictionary<string, Type>();
        private readonly bool IgnoreMissingTypes;

        public PlatformTypeProvider(bool ignoreMissingTypes) 
        {
            this.IgnoreMissingTypes = ignoreMissingTypes;
        }

        public string GetIdentifierForType(Type type) => type.AssemblyQualifiedName;

        public Type GetTypeForIdentifier(string identifier) => this.Cache.GetOrAdd(identifier, t => Type.GetType(t, throwOnError: !this.IgnoreMissingTypes));
    }
}
