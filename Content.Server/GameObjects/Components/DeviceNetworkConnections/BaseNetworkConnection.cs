using Content.Server.DeviceNetwork;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.DeviceNetworkConnections
{
    public abstract class BaseNetworkConnectionComponent : Component
    {
        [Dependency] private readonly DeviceNetwork.DeviceNetwork _network;

        private bool _receiveAll;

        protected abstract int DeviceNetID { get; }
        protected abstract int DeviceNetFrequency { get; }

        protected DeviceNetworkConnection Connection;

        [ViewVariables]
        public bool Open => Connection.Open;
        [ViewVariables]
        public string Address => Connection.Address;
        [ViewVariables]
        public int Frequency => Connection.Frequency;

        public override void Initialize()
        {
            base.Initialize();

            Connection = _network.Register(DeviceNetID, DeviceNetFrequency, OnReceiveDeviceNetMessage, _receiveAll);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _receiveAll, "ReceiveAll", false);

        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case ConnectionSendComponentMessage msg:
                    Send(msg.Frequency, msg.Address, msg.Payload);
                    break;
                case ConnectionBroadcastComponentMessage msg:
                    Broadcast(msg.Frequency, msg.Payload);
                    break;
            }
        }

        public override void OnRemove()
        {
            Connection.Close();
            base.OnRemove();
        }

        private bool Send(int frequency, string address, Dictionary<string, string> payload)
        {
            var data = ManipulatePayload(payload);
            var metadata = GetMetadata();
            return Connection.Send(frequency, address, data, metadata);
        }

        private bool Broadcast(int frequency, Dictionary<string, string> payload)
        {
            var data = ManipulatePayload(payload);
            var metadata = GetMetadata();
            return Connection.Broadcast(frequency, data, metadata);
        }

        private void OnReceiveDeviceNetMessage(int frequency, string sender, IReadOnlyDictionary<string, string> payload, Metadata metadata, bool broadcast)
        {
            if (CanReceive(frequency, sender, payload, metadata, broadcast))
            {
                SendMessage(new PacketReceivedComponentMessage(sender, payload, metadata, broadcast, frequency));
            }
        }

        protected abstract bool CanReceive(int frequency, string sender, IReadOnlyDictionary<string, string> payload, Metadata metadata, bool broadcast);
        protected abstract Dictionary<string, string> ManipulatePayload(Dictionary<string, string> payload);
        protected abstract Metadata GetMetadata();

        public class ConnectionSendComponentMessage : ComponentMessage
        {
            public readonly string Address;
            public readonly Dictionary<string, string> Payload;
            public readonly int Frequency;

            public ConnectionSendComponentMessage(string address, Dictionary<string, string> payload, int frequency = 0)
            {
                Address = address;
                Payload = payload;
                Frequency = frequency;
            }
        }

        public class ConnectionBroadcastComponentMessage : ComponentMessage
        {
            public readonly Dictionary<string, string> Payload;
            public readonly int Frequency;

            public ConnectionBroadcastComponentMessage(Dictionary<string, string> payload, int frequency = 0)
            {
                Payload = payload;
                Frequency = frequency;
            }
        }

        public class PacketReceivedComponentMessage : ComponentMessage
        {
            public readonly string Sender;
            public readonly IReadOnlyDictionary<string, string> Payload;
            public readonly Metadata Metadata;
            public readonly bool Broadcast;
            public readonly int Frequency;

            public PacketReceivedComponentMessage(string sender, IReadOnlyDictionary<string, string> payload, Metadata metadata, bool broadcast, int frequency = 0)
            {
                Sender = sender;
                Payload = payload;
                Metadata = metadata;
                Broadcast = broadcast;
                Frequency = frequency;
            }
        }
    }
}
