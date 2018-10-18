using System;
using System.Collections.Concurrent;

namespace Eveneum
{
    class TypeCache
    {
        private static readonly ConcurrentDictionary<string, Type> Cache = new ConcurrentDictionary<string, Type>();

        public Type Resolve(string type)
        {
            if (string.IsNullOrEmpty(type))
                return null;

            return Cache.GetOrAdd(type, t => Type.GetType(t));
        }
    }
}
