using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eveneum
{
    public interface IReadStream
    {
        Task<StreamResponse> ReadStream(string streamId, ReadStreamOptions options = default, CancellationToken cancellationToken = default);
        
        [Obsolete("Replaced by the version of ReadStream that accepts ReadStreamOptions")]
        Task<StreamResponse> ReadStreamAsOfVersion(string streamId, ulong version, CancellationToken cancellationToken = default);
        [Obsolete("Replaced by the version of ReadStream that accepts ReadStreamOptions")]
        Task<StreamResponse> ReadStreamFromVersion(string streamId, ulong version, CancellationToken cancellationToken = default);
        [Obsolete("Replaced by the version of ReadStream that accepts ReadStreamOptions")]
        Task<StreamResponse> ReadStreamIgnoringSnapshots(string streamId, CancellationToken cancellationToken = default);
    }

    public interface IWriteToStream
    {
        Task<Response> WriteToStream(string streamId, EventData[] events, ulong? expectedVersion = null, object metadata = null, CancellationToken cancellationToken = default);
    }

    public interface IDeleteStream
    {
        DeleteMode DeleteMode { get; }
        Task<DeleteResponse> DeleteStream(string streamId, ulong expectedVersion, CancellationToken cancellationToken = default);
    }

    public interface IManageSnapshots
    {
        Task<Response> CreateSnapshot(string streamId, ulong version, object snapshot, object metadata = null, bool deleteOlderSnapshots = false, CancellationToken cancellationToken = default);
        Task<DeleteResponse> DeleteSnapshots(string streamId, ulong olderThanVersion, CancellationToken cancellationToken = default);
    }

    public interface IEventStore : IReadStream, IWriteToStream, IDeleteStream, IManageSnapshots
    {
        Task Initialize(CancellationToken cancellationToken = default);
    }
}