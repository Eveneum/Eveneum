using System;

namespace Eveneum;

static class ExceptionData
{
    internal static T GetRequired<T>(this Exception exception, string key)
    {
        var value = exception.Data[key];

        return value is T typed
            ? typed
            : throw new InvalidOperationException($"The data of exception '{exception.GetType().Name}' does not contain a value for '{key}'.");
    }
}
