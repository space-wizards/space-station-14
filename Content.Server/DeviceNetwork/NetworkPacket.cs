using System.Collections.Generic;

namespace Content.Server.DeviceNetwork
{
    public class NetworkPayload : Dictionary<string, string>, IDictionary<string, string>
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
                if(data[index].Item1.Length <= MAX_STRING_SIZE && data[index].Item2.Length <= MAX_STRING_SIZE)
                {
                    packet.Add(data[index].Item1, data[index].Item2);
                }
            }
            return packet;
        }

    }
}
