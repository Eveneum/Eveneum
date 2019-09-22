using System;

namespace Eveneum
{
    [Serializable]
    public class NotInitializedException : Exception
    {
        public NotInitializedException() : base("EventStore hasn't been initialized. Please call EventStore.Initialize() method.")
        {
        }
    }
}
