using System;
using Eveneum.Documents;

namespace Eveneum.Serialization
{
    public class EveneumDocumentSerializer(IJsonSerializer jsonSerializer = null, ITypeProvider typeProvider = null, bool ignoreMissingTypes = false)
    {
        public IJsonSerializer JsonSerializer { get; } = jsonSerializer ?? new SystemTextJsonSerializer();
        public ITypeProvider TypeProvider { get; } = typeProvider ?? new PlatformTypeProvider(ignoreMissingTypes);

        public const char Separator = '~';

        public EventData DeserializeEvent(IEveneumDocument document)
        {
            var metadata = DeserializeObject(document.MetadataType, document.Metadata);
            var body = DeserializeObject(document.BodyType, document.Body);

            return new EventData(document.StreamId, body, metadata, document.Version, document.Timestamp, document.Deleted);
        }

        public Snapshot DeserializeSnapshot(IEveneumDocument document)
        {
            var metadata = DeserializeObject(document.MetadataType, document.Metadata);
            var body = DeserializeObject(document.BodyType, document.Body);

            return new Snapshot(body, metadata, document.Version);
        }

        internal void SerializeHeaderMetadata(IEveneumDocument header, object metadata)
        {
            if (metadata != null)
            {
                header.MetadataType = this.TypeProvider.GetIdentifierForType(metadata.GetType());
                header.Metadata = this.JsonSerializer.Serialize(metadata);
            }
        }

        internal IEveneumDocument SerializeEvent(EventData @event, string streamId)
        {
            var document = this.JsonSerializer.CreateDocument(GenerateEventId(streamId, @event.Version), DocumentType.Event);
            document.StreamId = streamId;
            document.Version = @event.Version;
            document.BodyType = this.TypeProvider.GetIdentifierForType(@event.Body.GetType());
            document.Body = this.JsonSerializer.Serialize(@event.Body);

            if (@event.Metadata != null)
            {
                document.MetadataType = this.TypeProvider.GetIdentifierForType(@event.Metadata.GetType());
                document.Metadata = this.JsonSerializer.Serialize(@event.Metadata);
            }

            return document;
        }

        internal IEveneumDocument SerializeSnapshot(object snapshot, object metadata, ulong version, string streamId, SnapshotMode snapshotMode)
        {
            var document = this.JsonSerializer.CreateDocument(GenerateSnapshotId(snapshotMode, streamId, version), DocumentType.Snapshot);
            document.StreamId = streamId;
            document.Version = version;
            document.BodyType = this.TypeProvider.GetIdentifierForType(snapshot.GetType());
            document.Body = this.JsonSerializer.Serialize(snapshot);

            if (metadata != null)
            {
                document.MetadataType = this.TypeProvider.GetIdentifierForType(metadata.GetType());
                document.Metadata = this.JsonSerializer.Serialize(metadata);
            }

            return document;
        }

        internal object DeserializeObject(string typeName, object data)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var type = this.TypeProvider.GetTypeForIdentifier(typeName);
            
            if (type is null)
            {
                if (ignoreMissingTypes)
                    return null;
                else
                    throw new TypeNotFoundException(typeName);
            }

            try
            {
                return this.JsonSerializer.Deserialize(data, type);
            }
            catch (Exception exc)
            {
                throw new JsonDeserializationException(typeName, data?.ToString() ?? string.Empty, exc);
            }
        }

        internal static string GenerateEventId(string streamId, ulong version) => $"{streamId}{Separator}{version}";

        internal static string GenerateSnapshotId(SnapshotMode snapshotMode, string streamId, ulong version) =>
            snapshotMode == SnapshotMode.Single
                ? $"{streamId}{Separator}S"
                : $"{streamId}{Separator}{version}{Separator}S";
    }
}
