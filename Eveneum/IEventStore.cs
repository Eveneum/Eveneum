using System.Threading.Tasks;

namespace Eveneum
{
    public interface IEventStore
    {
        Task<Stream> ReadStream(string streamId);
        Task WriteToStream(string streamId, object[] events, ulong expectedVersion = 0, object metadata = null);
        Task DeleteStream(Stream stream);
    }
}
