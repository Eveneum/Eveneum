using System;

namespace Eveneum
{
    [Serializable]
    public class NotInitializedException : EveneumException
    {
        public NotInitializedException() : base("EventStore hasn't been initialized. Please call EventStore.Initialize() method.")
        {
        }
    }
}
