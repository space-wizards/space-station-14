using Robust.Shared.Log;
using System.Collections.Generic;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.DeviceNetwork
{
    public class NetworkPayload : Dictionary<string, object>
    {
        //public const int MAX_STRING_SIZE = 100;

        private NetworkPayload(int size) : base(size)
        {
        }

        /// <summary>
        /// Crates a device network packet, drops invalid key value pairs
        /// </summary>
        /// <param name="data">An array of tuples containing key value pairs</param>
        /// <returns>The created device network packet</returns>
        public static NetworkPayload Create(params (string, object)[] data)
        {
            var packet = new NetworkPayload(data.Length);
            for (var index = 0; index < data.Length; index++)
            {
                //if (data[index].Item1.Length <= MAX_STRING_SIZE && data[index].Item2.Length <= MAX_STRING_SIZE)
                //{
                if (!packet.TryAdd(data[index].Item1, data[index].Item2))
                {
                    Logger.Error($"Duplicate device network payload entry: {data[index].Item1}");
                }
            }
            return packet;
        }

        /// <summary>
        /// Tries to get a value from the and checks if that value is of type T.
        /// </summary>
        /// <typeparam name="T">The type that sould be casted to</typeparam>
        /// <returns>Whether the value was present in the payload and of the required type</returns>
        public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value)
        {
            if (this.TryCastValue(key, out T result))
            {
                value = result;
                return true;
            }

            value = default;
            return false;
        }
    }

}
