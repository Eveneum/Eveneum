using Eveneum.Snapshots;
using System;
using System.Collections.Concurrent;

namespace Eveneum.Serialization
{
    public class PlatformTypeProvider : ITypeProvider
    {
		public const string SnapshotWriterSnapshotTypeIdentifier = "Eveneum.SnapshotWriterSnapshot";

		private readonly ConcurrentDictionary<string, Type> Cache = new ConcurrentDictionary<string, Type>();
        private readonly bool IgnoreMissingTypes;

        public PlatformTypeProvider(bool ignoreMissingTypes) 
        {
            this.IgnoreMissingTypes = ignoreMissingTypes;
        }

        public virtual string GetIdentifierForType(Type type) => type == typeof(SnapshotWriterSnapshot) ? SnapshotWriterSnapshotTypeIdentifier : type.AssemblyQualifiedName;

        public virtual Type GetTypeForIdentifier(string identifier) => identifier == SnapshotWriterSnapshotTypeIdentifier ? typeof(SnapshotWriterSnapshot) : this.Cache.GetOrAdd(identifier, t => Type.GetType(t, throwOnError: !this.IgnoreMissingTypes));
    }
}
