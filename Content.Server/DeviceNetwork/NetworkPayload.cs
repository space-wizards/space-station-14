using Robust.Shared.Log;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    public class NetworkPayload : Dictionary<string, string>
    {
        public const int MAX_STRING_SIZE = 100;

        private NetworkPayload(int size) : base(size)
        {
        }

        /// <summary>
        /// Crates a device network packet, drops invalid key value pairs
        /// </summary>
        /// <param name="data">An array of tuples containing key value pairs</param>
        /// <returns>The created device network packet</returns>
        public static NetworkPayload Create(params (string, string)[] data)
        {
            var packet = new NetworkPayload(data.Length);
            for (var index = 0; index < data.Length; index++)
            {
                if (data[index].Item1.Length <= MAX_STRING_SIZE && data[index].Item2.Length <= MAX_STRING_SIZE)
                {
                    if (!packet.TryAdd(data[index].Item1, data[index].Item2))
                    {
                        Logger.Error($"Duplicate device network payload entry: {data[index].Item1}");
                    }
                }
            }
            return packet;
        }

    }
}
