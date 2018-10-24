using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eveneum.Advanced
{
    public interface IAdvancedEventStore
    {
        Task LoadAllEvents(Action<IReadOnlyCollection<EventData>> callback, CancellationToken cancellationToken = default);
    }
}
