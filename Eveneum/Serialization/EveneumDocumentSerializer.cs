using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Eveneum.Documents;
using Eveneum.Snapshots;

namespace Eveneum.Serialization
{
    public class EveneumDocumentSerializer
    {
        public JsonSerializer JsonSerializer { get; }
        public ITypeProvider TypeProvider { get; }
        public bool IgnoreMissingTypes { get; }

        public const string SnapshotWriterSnapshotTypeIdentifier = "Eveneum.SnapshotWriterSnapshot";

        public EveneumDocumentSerializer(JsonSerializer jsonSerializer = null, ITypeProvider typeProvider = null, bool ignoreMissingTypes = false)
        {
            this.JsonSerializer = jsonSerializer ?? JsonSerializer.CreateDefault();
            this.TypeProvider = typeProvider ?? new PlatformTypeProvider(ignoreMissingTypes);
            this.IgnoreMissingTypes = ignoreMissingTypes;
        }

        public EventData DeserializeEvent(EveneumDocument document)
        {
            var metadata = DeserializeObject(document.MetadataType, document.Metadata);
            var body = DeserializeObject(document.BodyType, document.Body);

            return new EventData(document.StreamId, body, metadata, document.Version, document.Timestamp, document.Deleted);
        }

        public Snapshot DeserializeSnapshot(EveneumDocument document)
        {
            var metadata = DeserializeObject(document.MetadataType, document.Metadata);
            var body = DeserializeObject(document.BodyType, document.Body);

            return new Snapshot(body, metadata, document.Version);
        }

        internal void SerializeHeaderMetadata(EveneumDocument header, object metadata)
        {
            if (metadata != null)
            {
                header.MetadataType = this.GetIdentifierForType(metadata.GetType());
                header.Metadata = JToken.FromObject(metadata, this.JsonSerializer);
            }
        }

        internal EveneumDocument SerializeEvent(EventData @event, string streamId)
        {
            var document = new EveneumDocument(DocumentType.Event)
            {
                StreamId = streamId,
                Version = @event.Version,
                BodyType = this.GetIdentifierForType(@event.Body.GetType()),
                Body = JToken.FromObject(@event.Body, this.JsonSerializer)
            };

            if (@event.Metadata != null)
            {
                document.MetadataType = this.GetIdentifierForType(@event.Metadata.GetType());
                document.Metadata = JToken.FromObject(@event.Metadata, this.JsonSerializer);
            }

            return document;
        }

        internal EveneumDocument SerializeSnapshot(object snapshot, object metadata, ulong version, string streamId)
        {
            var document = new EveneumDocument(DocumentType.Snapshot)
            {
                StreamId = streamId,
                Version = version,
                BodyType = this.GetIdentifierForType(snapshot.GetType()),
                Body = JToken.FromObject(snapshot, this.JsonSerializer)
            };

            if (metadata != null)
            {
                document.MetadataType = this.GetIdentifierForType(metadata.GetType());
                document.Metadata = JToken.FromObject(metadata, this.JsonSerializer);
            }

            return document;
        }

        internal object DeserializeObject(string typeName, JToken data)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var type = this.GetTypeForIdentifier(typeName);
            
            if (type is null)
            {
                if (this.IgnoreMissingTypes)
                    return null;
                else
                    throw new TypeNotFoundException(typeName);
            }

            try
            {
                return data.ToObject(type, this.JsonSerializer);
            }
            catch (Exception exc)
            {
                throw new JsonDeserializationException(typeName, data.ToString(), exc);
            }
        }

        private string GetIdentifierForType(Type type) =>
            type == typeof(SnapshotWriterSnapshot) ? SnapshotWriterSnapshotTypeIdentifier : this.TypeProvider.GetIdentifierForType(type);

        private Type GetTypeForIdentifier(string identifier) =>
            identifier == SnapshotWriterSnapshotTypeIdentifier ? typeof(SnapshotWriterSnapshot) : this.TypeProvider.GetTypeForIdentifier(identifier);
    }
}
