using System.Threading.Tasks;
using System.Threading;

namespace Eveneum.Snapshots
{
    public interface ISnapshotWriter
    {
        Task<bool> CreateSnapshot(string streamId, ulong version, object snapshot, object metadata = null, CancellationToken cancellationToken = default);
        Task<Snapshot> ReadSnapshot(string streamId, ulong version, CancellationToken cancellationToken = default);
        Task DeleteSnapshots(string streamId, ulong olderThanVersion, CancellationToken cancellationToken = default);
    }
}
