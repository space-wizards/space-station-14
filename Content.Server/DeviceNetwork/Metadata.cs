using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.DeviceNetwork
{
    public static class Metadata
    {
        public static bool TryParseMetadata<T>(this Dictionary<string, object> metadata, string key, [NotNullWhen(true)] out T data)
        {
            if(metadata.TryGetValue(key, out var value) && value is T typedValue)
            {
                data = typedValue;
                return true;
            }

            data = default;
            return false;
        }
    }
}
