using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Popups;
using Content.Shared.Devices;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Serilog;

namespace Content.Server.Devices.Systems
{
    public class SignalerSystem : EntitySystem
    {

        [Dependency]
        private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

        private const string NET_SIGNALER_PING = "signaler_ping";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedSignalerComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<DeviceNetworkComponent, PacketSentEvent>(OnPacketReceived);
        }

        private void OnPacketReceived(EntityUid uid, DeviceNetworkComponent component, PacketSentEvent args)
        {
            //ignore signals form ourselves.
            if (args.SenderAddress == component.Address)
                return;

            if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            {
                //If that command is the PING command (you can check for your own command here)
                if (command == NET_SIGNALER_PING)
                {
                    var owner = EntityManager.GetEntity(uid);
                    owner.PopupMessageEveryone("The Signaler beeps.");
                }
            }
        }

        private void OnUse(EntityUid uid, SharedSignalerComponent component, UseInHandEvent args)
        {
            var signalerPacket = new NetworkPayload()
           {
               [DeviceNetworkConstants.Command] = NET_SIGNALER_PING,
           };
           _deviceNetworkSystem.QueuePacket(uid, "", component.Frequency, signalerPacket, true);
        }
    }
}
