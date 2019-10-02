using System.Threading;
using System.Threading.Tasks;

namespace Eveneum
{
    public interface IReadStream
    {
        Task<StreamResponse> ReadStream(string streamId, CancellationToken cancellationToken = default);
        Task<StreamResponse> ReadStreamAsOfVersion(string streamId, ulong version, CancellationToken cancellationToken = default);
        Task<StreamResponse> ReadStreamFromVersion(string streamId, ulong version, CancellationToken cancellationToken = default);
        Task<StreamResponse> ReadStreamIgnoringSnapshots(string streamId, CancellationToken cancellationToken = default);
    }

    public interface IWriteToStream
    {
        Task<Response> WriteToStream(string streamId, EventData[] events, ulong? expectedVersion = null, object metadata = null, CancellationToken cancellationToken = default);
    }

    public interface IDeleteStream
    {
        DeleteMode DeleteMode { get; }
        Task<Response> DeleteStream(string streamId, ulong expectedVersion, CancellationToken cancellationToken = default);
    }

    public interface IManageSnapshots
    {
        Task<Response> CreateSnapshot(string streamId, ulong version, object snapshot, object metadata = null, bool deleteOlderSnapshots = false, CancellationToken cancellationToken = default);
        Task<Response> DeleteSnapshots(string streamId, ulong olderThanVersion, CancellationToken cancellationToken = default);
    }

    public interface IEventStore : IReadStream, IWriteToStream, IDeleteStream, IManageSnapshots
    {
        Task Initialize();
    }
}