using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eveneum.Advanced
{
    public interface IAdvancedEventStore
    {
        Task<Response> LoadAllEvents(Func<IReadOnlyCollection<EventData>, Task> callback, CancellationToken cancellationToken = default);
        Task<Response> LoadEvents(string query, Func<IReadOnlyCollection<EventData>, Task> callback, CancellationToken cancellationToken = default);
    }
}
