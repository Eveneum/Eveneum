using System.Threading;
using System.Threading.Tasks;

namespace Eveneum
{
    public interface IReadStream
    {
        Task<Stream?> ReadStream(string streamId, CancellationToken cancellationToken = default);
        Task<Stream?> ReadStreamAsOfVersion(string streamId, ulong version, CancellationToken cancellationToken = default);
        Task<Stream?> ReadStreamIgnoringSnapshots(string streamId, CancellationToken cancellationToken = default);
    }

    public interface IWriteToStream
    {
        Task WriteToStream(string streamId, EventData[] events, ulong? expectedVersion = null, object metadata = null, CancellationToken cancellationToken = default);
    }

    public interface IDeleteStream
    {
        DeleteMode DeleteMode { get; }
        Task DeleteStream(string streamId, ulong expectedVersion, CancellationToken cancellationToken = default);
    }

    public interface IManageSnapshots
    {
        Task CreateSnapshot(string streamId, ulong version, object snapshot, object metadata = null, bool deleteOlderSnapshots = false, CancellationToken cancellationToken = default);
        Task DeleteSnapshots(string streamId, ulong olderThanVersion, CancellationToken cancellationToken = default);
    }

    public interface IEventStore : IReadStream, IWriteToStream, IDeleteStream, IManageSnapshots
    {
    }
}