using System.Threading;
using System.Threading.Tasks;

namespace Eveneum
{
    public interface IEventStore
    {
        DeleteMode DeleteMode { get; set; }
        Task<Stream?> ReadStream(string streamId, CancellationToken cancellationToken = default);
        Task WriteToStream(string streamId, EventData[] events, ulong? expectedVersion = null, object metadata = null, CancellationToken cancellationToken = default);
        Task DeleteStream(string streamId, ulong expectedVersion, CancellationToken cancellationToken = default);
        Task CreateSnapshot(string streamId, ulong version, object snapshot, object metadata = null, bool deleteOlderSnapshots = false, CancellationToken cancellationToken = default);
        Task DeleteSnapshots(string streamId, ulong olderThanVersion, CancellationToken cancellationToken = default);
    }
}
