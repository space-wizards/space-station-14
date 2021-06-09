using Content.Server.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    public abstract class BaseNetworkConnection : IDeviceNetworkConnection
    {
        protected readonly DeviceNetworkConnection Connection;

        protected OnReceiveNetMessage MessageHandler;

        [ViewVariables]
        public bool Open => Connection.Open;
        [ViewVariables]
        public string Address => Connection.Address;
        [ViewVariables]
        public int Frequency => Connection.Frequency;

        protected BaseNetworkConnection(int netId, int frequency, OnReceiveNetMessage onReceive, bool receiveAll)
        {
            var network = IoCManager.Resolve<IDeviceNetwork>();
            Connection = network.Register(netId, frequency, OnReceiveNetMessage, receiveAll);
            MessageHandler = onReceive;

        }

        public bool Send(int frequency, string address, Dictionary<string, string> payload)
        {
            var data = ManipulatePayload(payload);
            var metadata = GetMetadata();
            return Connection.Send(frequency, address, data, metadata);
        }

        public bool Send(string address, Dictionary<string, string> payload)
        {
            return Send(0, address, payload);
        }

        public bool Broadcast(int frequency, Dictionary<string, string> payload)
        {
            var data = ManipulatePayload(payload);
            var metadata = GetMetadata();
            return Connection.Broadcast(frequency, data, metadata);
        }

        public bool Broadcast(Dictionary<string, string> payload)
        {
            return Broadcast(0, payload);
        }

        public void Close()
        {
            Connection.Close();
        }

        private void OnReceiveNetMessage(int frequency, string sender, IReadOnlyDictionary<string, string> payload, Metadata metadata, bool broadcast)
        {
            if (CanReceive(frequency, sender, payload, metadata, broadcast))
            {
                MessageHandler(frequency, sender, payload, metadata, broadcast);
            }
        }

        protected abstract bool CanReceive(int frequency, string sender, IReadOnlyDictionary<string, string> payload, Metadata metadata, bool broadcast);
        protected abstract Dictionary<string, string> ManipulatePayload(Dictionary<string, string> payload);
        protected abstract Metadata GetMetadata();
    }
}
