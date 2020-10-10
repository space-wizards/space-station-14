using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.Interfaces
{
    public interface IDeviceNetworkConnection
    {
        public int Frequency { get; }
        public bool Send(int frequency, string address, Dictionary<string, string> payload);
        public bool Send(string address, Dictionary<string, string> payload);
        public bool Broadcast(int frequency, Dictionary<string, string> payload);
        public bool Broadcast(Dictionary<string, string> payload);
    }
}
