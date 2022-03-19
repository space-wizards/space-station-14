using System.Collections.Generic;
using System.Linq;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Exceptions;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.MachineLinking.System
{
    public sealed class SignalLinkerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalTransmitterComponent, InvokePortEvent>(OnTransmitterInvokePort);
            SubscribeLocalEvent<SignalTransmitterComponent, ComponentStartup>(OnTransmitterStartup);
            SubscribeLocalEvent<SignalTransmitterComponent, ComponentRemove>(OnTransmitterRemoved);
            SubscribeLocalEvent<SignalTransmitterComponent, InteractUsingEvent>(OnTransmitterInteractUsing);
            SubscribeLocalEvent<SignalTransmitterComponent, SignalPortSelected>(OnTransmitterSignalPortSelected);

            SubscribeLocalEvent<SignalReceiverComponent, ComponentRemove>(OnReceiverRemoved);
            SubscribeLocalEvent<SignalReceiverComponent, InteractUsingEvent>(OnReceiverInteractUsing);
            SubscribeLocalEvent<SignalReceiverComponent, SignalPortSelected>(OnReceiverSignalPortSelected);
        }

        private void OnTransmitterInvokePort(EntityUid uid, SignalTransmitterComponent component, InvokePortEvent args)
        {
            foreach (var receiver in component.Outputs[args.Port])
                RaiseLocalEvent(receiver.uid, new SignalReceivedEvent(receiver.port), false);
        }

        private void OnTransmitterStartup(EntityUid uid, SignalTransmitterComponent component, ComponentStartup args)
        {
            // validate links and give receivers a reference to their linked transmitter(s)
            foreach (var (transmitterPort, receivers) in component.Outputs)
                foreach (var receiver in receivers)
                    if (!TryComp(receiver.uid, out SignalReceiverComponent? receiverComponent) ||
                        !receiverComponent.Inputs.TryGetValue(receiver.port, out var transmitters))
                        receivers.Remove(receiver);
                    else if (!transmitters.Contains(new() { uid = uid, port = transmitterPort }))
                        receivers.Add(new() { uid = uid, port = transmitterPort });
        }

        private void OnTransmitterRemoved(EntityUid uid, SignalTransmitterComponent component, ComponentRemove args)
        {
            foreach (var (transmitterPort, receivers) in component.Outputs)
                foreach (var receiver in receivers)
                    if (TryComp(receiver.uid, out SignalReceiverComponent? receiverComponent) &&
                        receiverComponent.Inputs.TryGetValue(receiver.port, out var transmitters))
                        transmitters.Remove(new() { uid = uid, port = transmitterPort });
        }

        private void OnReceiverRemoved(EntityUid uid, SignalReceiverComponent component, ComponentRemove args)
        {
            foreach (var (receiverPort, transmitters) in component.Inputs)
                foreach (var transmitter in transmitters)
                    if (TryComp(transmitter.uid, out SignalTransmitterComponent? transmitterComponent) &&
                        transmitterComponent.Outputs.TryGetValue(transmitter.port, out var receivers))
                        receivers.Remove(new() { uid = uid, port = receiverPort });
        }

        private void OnTransmitterInteractUsing(EntityUid uid, SignalTransmitterComponent component, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!TryComp(args.Used, out SignalLinkerComponent? linker) ||
                !TryComp(args.User, out ActorComponent? actor))
                return;

            if (component.Outputs.Count == 1)
            {
                var port = component.Outputs.Keys.First();
                LinkerTransmitterInteraction(args.User, linker, component, port);
                args.Handled = true;
                return;
            }

            if (_userInterfaceSystem.TryGetUi(uid, SignalTransmitterUiKey.Key, out var bui))
            {
                bui.Open(actor.PlayerSession);
                bui.SetState(new SignalPortsState(component.Outputs.Keys.ToArray()));
                args.Handled = true;
                return;
            }
        }

        private void OnReceiverInteractUsing(EntityUid uid, SignalReceiverComponent component, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!TryComp(args.Used, out SignalLinkerComponent? linker) || !linker.savedPort.HasValue ||
                !TryComp(args.User, out ActorComponent? actor))
                return;

            if (component.Inputs.Count == 1)
            {
                var port = component.Inputs.Keys.First();
                LinkerReceiverInteraction(args.User, linker, component, port);
                args.Handled = true;
                return;
            }

            if (_userInterfaceSystem.TryGetUi(uid, SignalReceiverUiKey.Key, out var bui))
            {
                bui.Open(actor.PlayerSession);
                bui.SetState(new SignalPortsState(component.Inputs.Keys.ToArray()));
                args.Handled = true;
                return;
            }
        }

        private void OnTransmitterSignalPortSelected(EntityUid uid, SignalTransmitterComponent component, SignalPortSelected args)
        {
            if (args.Session.AttachedEntity is { } attached &&
                attached != default &&
                TryComp(attached, out HandsComponent? hands) &&
                hands.ActiveHandEntity is EntityUid heldEntity &&
                TryComp(heldEntity, out SignalLinkerComponent? signalLinkerComponent))
                LinkerTransmitterInteraction(attached, signalLinkerComponent, component, args.Port);
        }

        private void OnReceiverSignalPortSelected(EntityUid uid, SignalReceiverComponent component, SignalPortSelected args)
        {
            if (args.Session.AttachedEntity is { } attached &&
                attached != default &&
                TryComp(attached, out HandsComponent? hands) &&
                hands.ActiveHandEntity is EntityUid heldEntity &&
                TryComp(heldEntity, out SignalLinkerComponent? signalLinkerComponent))
                LinkerReceiverInteraction(attached, signalLinkerComponent, component, args.Port);
        }

        private void LinkerTransmitterInteraction(EntityUid entity, SignalLinkerComponent linkerComponent,
            SignalTransmitterComponent transmitter, string transmitterPort)
        {
            linkerComponent.savedPort = new() { uid = transmitter.Owner, port = transmitterPort };
            entity.PopupMessageCursor(Loc.GetString("signal-linker-component-saved-port",
                    ("port", transmitterPort), ("machine", transmitter.Owner)));
        }

        private void LinkerReceiverInteraction(EntityUid entity, SignalLinkerComponent linker,
            SignalReceiverComponent receiver, string receiverPort)
        {
            if (!linker.savedPort.HasValue) return;
            var identifier = linker.savedPort.Value;
            if (!TryComp(identifier.uid, out SignalTransmitterComponent? transmitter)) return;
            if (!transmitter.Outputs.TryGetValue(identifier.port, out var receivers)) return;
            if (!receiver.Inputs.TryGetValue(receiverPort, out var transmitters)) return;

            if (receivers.Contains(new() { uid = receiver.Owner, port = receiverPort }))
            { // link already exists, remove it
                if (receivers.Remove(new() { uid = receiver.Owner, port = receiverPort }))
                {
                    RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(receiverPort));
                    RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(identifier.port));
                    entity.PopupMessageCursor(Loc.GetString("signal-linker-component-unlinked-port",
                        ("port", receiverPort), ("machine", receiver)));
                }
            }
            else
            {
                if (!IsInRange(transmitter, receiver))
                {
                    entity.PopupMessageCursor(Loc.GetString("signal-linker-component-out-of-range"));
                    return;
                }

                var linkAttempt = new LinkAttemptEvent(entity, transmitter, identifier.port, receiver, receiverPort);
                RaiseLocalEvent(receiver.Owner, linkAttempt);
                RaiseLocalEvent(transmitter.Owner, linkAttempt);

                if (linkAttempt.Cancelled) return;

                receivers.Add(new() { uid = receiver.Owner, port = receiverPort });
                transmitters.Add(new() { uid = transmitter.Owner, port = identifier.port });

                entity.PopupMessageCursor(Loc.GetString("signal-linker-component-linked-port",
                    ("port", receiverPort), ("machine", receiver.Owner)));
            }
        }

        private bool IsInRange(SignalTransmitterComponent transmitterComponent,
            SignalReceiverComponent receiverComponent)
        {
            if (EntityManager.TryGetComponent<ApcPowerReceiverComponent?>(transmitterComponent.Owner, out var transmitterPowerReceiverComponent) &&
                EntityManager.TryGetComponent<ApcPowerReceiverComponent?>(receiverComponent.Owner, out var receiverPowerReceiverComponent)
            ) //&& todo are they on the same powernet?
            {
                return true;
            }

            return EntityManager.GetComponent<TransformComponent>(transmitterComponent.Owner).MapPosition.InRange(
                EntityManager.GetComponent<TransformComponent>(receiverComponent.Owner).MapPosition, 30f);
        }
    }
}
