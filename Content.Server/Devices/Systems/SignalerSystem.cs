using Content.Server.Clothing.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Devices;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
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
                //Make sure the command is a signaler ping.
                if (command == NET_SIGNALER_PING)
                {
                    var owner = EntityManager.GetEntity(uid);
                    if (owner.TryGetContainer(out var container))
                    {
                        var viewer = container.Owner;
                        viewer.PopupMessage(viewer, "You feel your signaler vibrate.");
                    }
                    else
                    {
                        owner.PopupMessageEveryone("BZZzzzz...", null, 5);
                    }
                }
            }
        }

        private void OnCanisterInteractHand(EntityUid uid, SharedSignalerComponent component, InteractHandEvent args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            component.Owner.GetUIOrNull(SignalerUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void OnUse(EntityUid uid, SharedSignalerComponent component, UseInHandEvent args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            var boundInterface = component.Owner.GetUIOrNull(SignalerUiKey.Key);
            boundInterface?.Open(actor.PlayerSession);

            args.Handled = true;

            return;

            var signalerPacket = new NetworkPayload()
           {
               [DeviceNetworkConstants.Command] = NET_SIGNALER_PING,
           };
           _deviceNetworkSystem.QueuePacket(uid, "", component.Frequency, signalerPacket, true);

           args.Handled = true;
        }
    }
}
