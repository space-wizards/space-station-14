using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    public class Metadata : Dictionary<string, object>
    {
        public bool TryParseMetadata<T>(string key, [NotNullWhen(true)] out T? data)
        {
            if (TryGetValue(key, out var value) && value is T typedValue)
            {
                data = typedValue;
                return true;
            }

            data = default;
            return false;
        }
    }
}
