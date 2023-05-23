using Eveneum.Snapshots;
using System;
using System.Collections.Concurrent;

namespace Eveneum.Serialization
{
    class PlatformTypeProvider : ITypeProvider
    {
		public const string SnapshotWriterSnapshotTypeIdentifier = "Eveneum.SnapshotWriterSnapshot";

		private readonly ConcurrentDictionary<string, Type> Cache = new ConcurrentDictionary<string, Type>();
        private readonly bool IgnoreMissingTypes;

        public PlatformTypeProvider(bool ignoreMissingTypes) 
        {
            this.IgnoreMissingTypes = ignoreMissingTypes;
        }

        public string GetIdentifierForType(Type type) => type == typeof(SnapshotWriterSnapshot) ? SnapshotWriterSnapshotTypeIdentifier : type.AssemblyQualifiedName;

        public Type GetTypeForIdentifier(string identifier) => identifier == SnapshotWriterSnapshotTypeIdentifier ? typeof(SnapshotWriterSnapshot) : this.Cache.GetOrAdd(identifier, t => Type.GetType(t, throwOnError: !this.IgnoreMissingTypes));
    }
}
