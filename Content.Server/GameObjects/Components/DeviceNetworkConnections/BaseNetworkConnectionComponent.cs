using Content.Server.DeviceNetwork;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.DeviceNetworkConnections
{
    public abstract class BaseNetworkConnectionComponent : Component
    {
        [Dependency] private readonly IDeviceNetwork _network = default!;

        private const int UNDEFINED = -1;

        private bool _receiveAll;
        private bool _handlePings;
        private string _pingResponse;

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
            serializer.DataField(ref _handlePings, "RespondToPings", false);
            serializer.DataField(ref _pingResponse, "PingResponse", "");
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            //Those if statements ensure that the message was meant to be sent from this connection.
            switch (message)
            {
                case SendComponentMessage msg:

                    if(msg.NetID == UNDEFINED || msg.NetID == DeviceNetID )
                        Send(msg.Frequency == UNDEFINED ? Frequency : msg.Frequency, msg.Address, msg.Payload);

                    break;
                case BroadcastComponentMessage msg:

                    if (msg.NetID == UNDEFINED || msg.NetID == DeviceNetID)
                        Broadcast(msg.Frequency == UNDEFINED ? Frequency : msg.Frequency, msg.Payload);

                    break;
            }
        }

        public override void OnRemove()
        {
            Connection.Close();
            base.OnRemove();
        }

        private bool Send(int frequency, string address, NetworkPayload payload)
        {
            var data = ManipulatePayload(payload);
            var metadata = GetMetadata();
            return Connection.Send(frequency, address, data, metadata);
        }

        private bool Broadcast(int frequency, NetworkPayload payload)
        {
            var data = ManipulatePayload(payload);
            var metadata = GetMetadata();
            return Connection.Broadcast(frequency, data, metadata);
        }

        private void HandlePing(int frequency, string sender, NetworkPayload payload)
        {
            if (_handlePings)
            {
                NetworkUtils.PingResponse(frequency, Connection, sender, payload, _pingResponse);
            }
        }

        private void OnReceiveDeviceNetMessage(int frequency, string sender, NetworkPayload payload, Dictionary<string, object> metadata, bool broadcast)
        {
            if (CanReceive(frequency, sender, payload, metadata, broadcast))
            {
                HandlePing(frequency, sender, payload);
                SendMessage(new PacketReceivedComponentMessage(sender, payload, metadata, broadcast, frequency, Address, DeviceNetID, Frequency));
            }
        }

        protected abstract bool CanReceive(int frequency, string sender, NetworkPayload payload, Dictionary<string, object> metadata, bool broadcast);
        protected abstract NetworkPayload ManipulatePayload(NetworkPayload payload);

        /// <summary>
        /// Collect the mesages metadata. Information that is required but not part of the message itself.
        /// </summary>
        /// <returns></returns>
        protected abstract Dictionary<string, object> GetMetadata();


        /// <summary>
        /// The connection component sends the <see cref="NetworkPayload"/> to the specified address if it receives this message.
        /// </summary>
        public class SendComponentMessage : ComponentMessage
        {
            public string Address { get; }
            public NetworkPayload Payload { get; }
            public int Frequency { get; }
            public int NetID { get; }

            public SendComponentMessage(string address, NetworkPayload payload, int frequency = UNDEFINED, int netID = UNDEFINED)
            {
                Address = address;
                Payload = payload;
                Frequency = frequency;
                NetID = netID;
            }
        }

        /// <summary>
        /// The connection component sends the <see cref="NetworkPayload"/> to all addresses on the same network
        /// </summary>
        public class BroadcastComponentMessage : ComponentMessage
        {
            public NetworkPayload Payload { get; }
            public int Frequency { get; }
            public int NetID { get;  }

            public BroadcastComponentMessage(NetworkPayload payload, int frequency = UNDEFINED, int netID = UNDEFINED)
            {
                Payload = payload;
                Frequency = frequency;
                NetID = netID;
            }
        }

        /// <summary>
        /// This component message gets sent when the connection component receives a device network packet.
        /// </summary>
        public class PacketReceivedComponentMessage : ComponentMessage
        {
            public string Sender { get; }
            public NetworkPayload Payload { get; }
            public Dictionary<string, object> Metadata { get; }
            public bool Broadcast { get; }
            public int Frequency { get; }
            public string OwnAddress { get; }
            public int OwnNetID { get; }
            public int OwnFrequency { get; }

            public PacketReceivedComponentMessage(string sender, NetworkPayload payload, Dictionary<string, object> metadata, bool broadcast, int frequency, string ownAddress, int ownNetID, int ownFrequency)
            {
                Sender = sender;
                Payload = payload;
                Metadata = metadata;
                Broadcast = broadcast;
                Frequency = frequency;
                OwnAddress = ownAddress;
                OwnNetID = ownNetID;
                OwnFrequency = ownFrequency;
            }
        }
    }
}
