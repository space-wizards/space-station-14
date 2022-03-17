using System.Collections.Generic;
using System.Linq;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Exceptions;
using Content.Server.MachineLinking.Models;
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
            var port = component.Outputs.GetPort(args.Port);
            foreach (var receiver in port.Receivers)
                RaiseLocalEvent(receiver.entity, new SignalReceivedEvent(receiver.port), false);
        }

        private void OnTransmitterStartup(EntityUid uid, SignalTransmitterComponent component, ComponentStartup args)
        {
            // validate links and give receivers a reference to their linked transmitter(s)
            foreach (var transmitterPort in component.Outputs)
                foreach (var (receiver, receiverPortName) in transmitterPort.Receivers)
                    if (!TryComp(receiver, out SignalReceiverComponent? receiverComponent) ||
                        !receiverComponent.Inputs.TryGetPort(receiverPortName, out var receiverPort))
                        transmitterPort.Receivers.Remove((receiver, receiverPortName));
                    else if (!receiverPort.Transmitters.Contains(transmitterPort))
                        receiverPort.Transmitters.Add(transmitterPort);
        }

        private void OnTransmitterRemoved(EntityUid uid, SignalTransmitterComponent component, ComponentRemove args)
        {
            foreach (var transmitterPort in component.Outputs)
                foreach (var (receiver, receiverPortName) in transmitterPort.Receivers)
                    if (TryComp(receiver, out SignalReceiverComponent? receiverComponent) &&
                        receiverComponent.Inputs.TryGetPort(receiverPortName, out var receiverPort))
                        receiverPort.Transmitters.Remove(transmitterPort);
        }

        private void OnReceiverRemoved(EntityUid uid, SignalReceiverComponent component, ComponentRemove args)
        {
            foreach (var receiverPort in component.Inputs)
                foreach (var transmitterPort in receiverPort.Transmitters)
                    transmitterPort.Receivers.Remove((uid, receiverPort.Name));
        }

        private void OnTransmitterInteractUsing(EntityUid uid, SignalTransmitterComponent component, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!TryComp(args.Used, out SignalLinkerComponent? linker) ||
                !TryComp(args.User, out ActorComponent? actor))
                return;

            if (component.Outputs.Count == 1)
            {
                var port = component.Outputs.First();
                LinkerTransmitterInteraction(args.User, linker, component, port.Name);
                args.Handled = true;
                return;
            }

            if (_userInterfaceSystem.TryGetUi(uid, SignalTransmitterUiKey.Key, out var bui))
            {
                bui.Open(actor.PlayerSession);
                bui.SetState(new SignalPortsState(component.Outputs.Select(port => port.Name).ToArray()));
                args.Handled = true;
                return;
            }
        }

        private void OnReceiverInteractUsing(EntityUid uid, SignalReceiverComponent component, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!TryComp(args.Used, out SignalLinkerComponent? linker) || !linker.savedPort.HasValue ||
                !TryComp(args.User, out ActorComponent? actor) ||
                !linker.savedPort.Value.transmitter.Outputs.TryGetPort(linker.savedPort.Value.port, out var port))
                return;

            if (component.Inputs.Count == 1)
            {
                LinkerReceiverInteraction(args.User, linker, component, component.Inputs.First().Name);
                args.Handled = true;
                return;
            }

            if (_userInterfaceSystem.TryGetUi(uid, SignalReceiverUiKey.Key, out var bui))
            {
                bui.Open(actor.PlayerSession);
                bui.SetState(new SignalPortsState(component.Inputs.Select(port => port.Name).ToArray()));
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
            SignalTransmitterComponent transmitter, string portName)
        {
            var port = transmitter.Outputs.GetPort(portName);
            linkerComponent.savedPort = (transmitter, portName);
            entity.PopupMessageCursor(Loc.GetString("signal-linker-component-saved-port",
                    ("port", portName), ("machine", transmitter.Owner)));
        }

        private void LinkerReceiverInteraction(EntityUid entity, SignalLinkerComponent linker,
            SignalReceiverComponent receiver, string receiverPortName)
        {
            if (!linker.savedPort.HasValue) return;
            var (transmitter, transmitterPortName) = linker.savedPort.Value;
            if (!transmitter.Outputs.TryGetPort(transmitterPortName, out var transmitterPort)) return;
            if (!receiver.Inputs.TryGetPort(receiverPortName, out var receiverPort)) return;

            if (transmitterPort.Receivers.Contains((receiver.Owner, receiverPortName)))
            { // link already exists, remove it
                if (transmitterPort.Receivers.Remove((receiver.Owner, receiverPortName)))
                {
                    RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(receiverPortName));
                    RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(transmitterPortName));
                    entity.PopupMessageCursor(Loc.GetString("signal-linker-component-unlinked-port",
                        ("port", receiverPortName), ("machine", receiver)));
                }
            }
            else
            {
                if (!IsInRange(transmitter, receiver))
                {
                    entity.PopupMessageCursor(Loc.GetString("signal-linker-component-out-of-range"));
                    return;
                }

                var linkAttempt = new LinkAttemptEvent(entity, transmitter, transmitterPortName, receiver, receiverPortName);
                RaiseLocalEvent(receiver.Owner, linkAttempt);
                RaiseLocalEvent(transmitter.Owner, linkAttempt);

                if (linkAttempt.Cancelled) return;

                transmitterPort.Receivers.Add((receiver.Owner, receiverPortName));
                receiverPort.Transmitters.Add(transmitterPort);

                entity.PopupMessageCursor(Loc.GetString("signal-linker-component-linked-port",
                    ("port", receiverPortName), ("machine", receiver.Owner)));
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
