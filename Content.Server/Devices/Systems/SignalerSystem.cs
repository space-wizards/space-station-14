using System;
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
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Serilog;

namespace Content.Server.Devices.Systems
{
    public class SignalerSystem : EntitySystem
    {

        [Dependency]
        private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

        [Dependency]
        private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        private const string NET_SIGNALER_PING = "signaler_ping";

        public override void Initialize()
        {
            SubscribeLocalEvent<SharedSignalerComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<DeviceNetworkComponent, PacketSentEvent>(OnPacketReceived);
            SubscribeLocalEvent<SharedSignalerComponent, GetOtherVerbsEvent>(AddTuneVerb);
            SubscribeLocalEvent<SharedSignalerComponent, ComponentStartup>(SyncInitialFrequency);
            SubscribeLocalEvent<SharedSignalerComponent, IoDeviceInputEvent>(OnIOReceived);

            // Bound UI subscriptions
            SubscribeLocalEvent<SharedSignalerComponent, SignalerSendSignalMessage>(OnSendSignalRequest);
            SubscribeLocalEvent<SharedSignalerComponent, SignalerUpdateFrequencyMessage>(UpdateFrequency);
        }

        //when we receive an IO input, we activate the signaler.
        private void OnIOReceived(EntityUid uid, SharedSignalerComponent component, IoDeviceInputEvent args)
        {
            SendSignal(uid);
        }

        private void SyncInitialFrequency(EntityUid uid, SharedSignalerComponent component, ComponentStartup args)
        {
            SetFrequency(uid, component.Frequency, component);
        }

        private void AddTuneVerb(EntityUid uid, SharedSignalerComponent component, GetOtherVerbsEvent args)
        {
            if (!args.CanAccess)
                return;

            Verb verb = new();
            verb.Act = () =>
            {
                if (!EntityManager.TryGetComponent<ActorComponent>(args.User.Uid, out var actorComponent))
                    return;
                _userInterfaceSystem.TryOpen(uid, SignalerUiKey.Key, actorComponent.PlayerSession);
            };
            verb.Text = Loc.GetString("signaler-component-tune-verb");
            args.Verbs.Add(verb);
        }

        private void OnSendSignalRequest(EntityUid uid, SharedSignalerComponent component, SignalerSendSignalMessage args)
        {
            SendSignal(uid);
        }

        private void UpdateFrequency(EntityUid uid, SharedSignalerComponent component, SignalerUpdateFrequencyMessage args)
        {
            var freq = Math.Clamp(args.Frequency, SharedSignalerComponent.MinFrequency,
                SharedSignalerComponent.MaxFrequency);

            //if someone is holding the signaler, let them know it updated the frequency
            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetContainer(out var container))
            {
                var viewer = container.Owner;
                viewer.PopupMessage(viewer, Loc.GetString("signaler-component-update-acknowledgement"));
            }

            SetFrequency(uid, freq, component);
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
                        viewer.PopupMessage(viewer, Loc.GetString("signaler-component-owner-notify"));

                        //if the device is in a container, try to apply an output signal to that container.
                        //i'm not a big fan of this, but I can't think of an ideal way to do this other than maybe
                        //a broadcast event? which then manually checks if the device is connected to this one?
                        //dunno.
                        RaiseLocalEvent(container.Owner.Uid, new IoDeviceOutputEvent());
                    }
                    else
                    {
                        owner.PopupMessageEveryone(Loc.GetString("signaler-component-generic-notify"), null, 5);
                    }

                }
            }
        }

        public void SetFrequency(EntityUid uid, int frequency, SharedSignalerComponent? signalerComponent = null,
            DeviceNetworkComponent? networkComponent = null)
        {
            if (!Resolve(uid, ref signalerComponent, ref networkComponent))
                return;

            //clamp frequency between min and max values.
            frequency = Math.Clamp(frequency, SharedSignalerComponent.MinFrequency,
                SharedSignalerComponent.MaxFrequency);

            signalerComponent.Frequency = frequency;
            networkComponent.Frequency = frequency;

            _userInterfaceSystem.TrySetUiState(uid, SignalerUiKey.Key, new SignalerBoundUserInterfaceState(frequency));
        }

        public void SendSignal(EntityUid uid, SharedSignalerComponent? signalerComponent = null)
        {
            if (!Resolve(uid, ref signalerComponent))
                return;

            var signalerPacket = new NetworkPayload()
            {
                [DeviceNetworkConstants.Command] = NET_SIGNALER_PING,
            };
            _deviceNetworkSystem.QueuePacket(uid, "", signalerComponent.Frequency, signalerPacket, true);
        }

        private void OnUse(EntityUid uid, SharedSignalerComponent component, UseInHandEvent args)
        {
            SendSignal(uid, component);
        }

    }
}
