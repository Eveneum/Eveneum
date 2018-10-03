using System.Threading.Tasks;

namespace Eveneum
{
    public interface IEventStore
    {
        Task<Stream> ReadStream(string streamId);
        Task WriteToStream(string streamId, object[] events, ulong expectedVersion = 0, object metadata = null);
        Task DeleteStream(string streamId, ulong expectedVersion);
        Task WriteSnapshot(string streamId, ulong version, object snapshot, bool deletePrevious = false);
        Task DeleteSnapshots(string streamId, ulong olderThanVersion);
    }
}
