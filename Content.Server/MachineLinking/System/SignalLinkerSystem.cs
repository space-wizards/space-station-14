using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
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

            SubscribeLocalEvent<SignalReceiverComponent, ComponentRemove>(OnReceiverRemoved);
            SubscribeLocalEvent<SignalReceiverComponent, InteractUsingEvent>(OnReceiverInteractUsing);

            SubscribeLocalEvent<SignalLinkerComponent, SignalPortSelected>(OnSignalPortSelected);
            SubscribeLocalEvent<SignalLinkerComponent, LinkerClearSelected>(OnLinkerClearSelected);
            SubscribeLocalEvent<SignalLinkerComponent, LinkerLinkDefaultSelected>(OnLinkerLinkDefaultSelected);
            SubscribeLocalEvent<SignalLinkerComponent, BoundUIClosedEvent>(OnLinkerUIClosed);
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
                        receivers.Remove(receiver); // TODO log error
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

        private void OnTransmitterInteractUsing(EntityUid uid, SignalTransmitterComponent transmitter, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!TryComp(args.Used, out SignalLinkerComponent? linker) ||
                !TryComp(args.User, out ActorComponent? actor))
                return;

            linker.savedTransmitter = uid;

            if (!TryComp(linker.savedReceiver, out SignalReceiverComponent? receiver))
            {
                args.User.PopupMessageCursor(Loc.GetString("signal-linker-component-saved", ("machine", uid)));
                args.Handled = true;
                return;
            }

            if (TryOpenUI(actor, linker, out var bui))
            {
                TryUpdateUI(linker, transmitter, receiver, bui);
                args.Handled = true;
                return;
            }
        }

        private void OnReceiverInteractUsing(EntityUid uid, SignalReceiverComponent receiver, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!TryComp(args.Used, out SignalLinkerComponent? linker) ||
                !TryComp(args.User, out ActorComponent? actor))
                return;

            linker.savedReceiver = uid;

            if (!TryComp(linker.savedTransmitter, out SignalTransmitterComponent? transmitter))
            {
                args.User.PopupMessageCursor(Loc.GetString("signal-linker-component-saved", ("machine", uid)));
                args.Handled = true;
                return;
            }

            if (TryOpenUI(actor, linker, out var bui))
            {
                TryUpdateUI(linker, transmitter, receiver, bui);
                args.Handled = true;
                return;
            }
        }

        private bool TryOpenUI(ActorComponent actor, SignalLinkerComponent linker, [NotNullWhen(true)] out BoundUserInterface? bui)
        {
            if (_userInterfaceSystem.TryGetUi(linker.Owner, SignalLinkerUiKey.Key, out bui))
                return bui.Open(actor.PlayerSession);
            return false;
        }

        private bool TryUpdateUI(SignalLinkerComponent linker, SignalTransmitterComponent transmitter, SignalReceiverComponent receiver, BoundUserInterface? bui = null)
        {
            if (bui == null && !_userInterfaceSystem.TryGetUi(linker.Owner, SignalLinkerUiKey.Key, out bui))
                return false;

            var outKeys = transmitter.Outputs.Keys.ToList();
            var inKeys = receiver.Inputs.Keys.ToList();
            // TODO this could probably be rewritten nicely with linq
            List<(int, int)> links = new();
            foreach (var (ok, i) in outKeys.Select((s, i) => (s, i)))
                foreach (var re in transmitter.Outputs[ok])
                    if (re.uid == receiver.Owner)
                        links.Add((i, inKeys.IndexOf(re.port)));

            bui.SetState(new SignalPortsState($"{Name(transmitter.Owner)} ({transmitter.Owner})", outKeys,
                $"{Name(receiver.Owner)} ({receiver.Owner})", inKeys, links));
            return true;

        }

        private bool TryLink(SignalTransmitterComponent transmitter, SignalReceiverComponent receiver, SignalPortSelected args, EntityUid? popupUid = null)
        {
            if (!transmitter.Outputs.TryGetValue(args.TransmitterPort, out var receivers) ||
                !receiver.Inputs.TryGetValue(args.ReceiverPort, out var transmitters))
                return false;

            if (!IsInRange(transmitter, receiver))
            {
                popupUid?.PopupMessageCursor(Loc.GetString("signal-linker-component-out-of-range"));
                return false;
            }

            // allow other systems to refuse the connection
            var linkAttempt = new LinkAttemptEvent(transmitter, args.TransmitterPort, receiver, args.ReceiverPort);
            RaiseLocalEvent(transmitter.Owner, linkAttempt);
            if (linkAttempt.Cancelled)
            {
                popupUid?.PopupMessageCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", transmitter.Owner)));
                return false;
            }
            RaiseLocalEvent(receiver.Owner, linkAttempt);
            if (linkAttempt.Cancelled)
            {
                popupUid?.PopupMessageCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", receiver.Owner)));
                return false;
            }

            receivers.Add(new() { uid = receiver.Owner, port = args.ReceiverPort });
            transmitters.Add(new() { uid = transmitter.Owner, port = args.TransmitterPort });
            popupUid?.PopupMessageCursor(Loc.GetString("signal-linker-component-linked-port",
                ("machine1", transmitter.Owner), ("port1", args.TransmitterPort),
                ("machine2", receiver.Owner), ("port2", args.ReceiverPort)));

            return true;
        }

        private void OnSignalPortSelected(EntityUid uid, SignalLinkerComponent linker, SignalPortSelected args)
        {
            if (!TryComp(linker.savedTransmitter, out SignalTransmitterComponent? transmitter) ||
                !TryComp(linker.savedReceiver, out SignalReceiverComponent? receiver) ||
                !transmitter.Outputs.TryGetValue(args.TransmitterPort, out var receivers) ||
                !receiver.Inputs.TryGetValue(args.ReceiverPort, out var transmitters))
                return;

            if (args.Session.AttachedEntity is not EntityUid attached || attached == default ||
                !TryComp(attached, out ActorComponent? actor))
                return;

            if (receivers.Contains(new() { uid = receiver.Owner, port = args.ReceiverPort }) ||
                transmitters.Contains(new() { uid = transmitter.Owner, port = args.TransmitterPort }))
            { // link already exists, remove it
                if (receivers.Remove(new() { uid = receiver.Owner, port = args.ReceiverPort }) &&
                    transmitters.Remove(new() { uid = transmitter.Owner, port = args.TransmitterPort }))
                {
                    RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(args.ReceiverPort));
                    RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(args.TransmitterPort));
                    attached.PopupMessageCursor(Loc.GetString("signal-linker-component-unlinked-port",
                        ("machine1", transmitter.Owner), ("port1", args.TransmitterPort),
                        ("machine2", receiver.Owner), ("port2", args.ReceiverPort)));
                }
                else
                { // something weird happened
                  // TODO log error
                }
            }
            else
            {
                TryLink(transmitter, receiver, args, attached);
            }
            
            TryUpdateUI(linker, transmitter, receiver);
        }

        private void OnLinkerClearSelected(EntityUid uid, SignalLinkerComponent linker, LinkerClearSelected args)
        {
            if (!TryComp(linker.savedTransmitter, out SignalTransmitterComponent? transmitter) ||
                !TryComp(linker.savedReceiver, out SignalReceiverComponent? receiver))
                return;

            foreach (var (port, receivers) in transmitter.Outputs)
                if (receivers.RemoveAll(id => id.uid == receiver.Owner) > 0)
                    RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(port));

            foreach (var (port, transmitters) in receiver.Inputs)
                if (transmitters.RemoveAll(id => id.uid == transmitter.Owner) > 0)
                    RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(port));

            TryUpdateUI(linker, transmitter, receiver);
        }

        private void OnLinkerLinkDefaultSelected(EntityUid uid, SignalLinkerComponent linker, LinkerLinkDefaultSelected args)
        {
            List<List<(string, string)>> mappings = new()
            {
                new() { ("Pressed", "Toggle") },
                new() { ("On", "On"), ("Off", "Off") },
                new() { ("On", "Open"), ("Off", "Close") },
                new() { ("On", "Forward"), ("Off", "Off") },
                new() { ("Left", "On"), ("Right", "On"), ("Middle", "Off") },
                new() { ("Left", "Open"), ("Right", "Open"), ("Middle", "Close") },
                new() { ("Left", "Forward"), ("Right", "Reverse"), ("Middle", "Off") },
            };

            if (!TryComp(linker.savedTransmitter, out SignalTransmitterComponent? transmitter) ||
                !TryComp(linker.savedReceiver, out SignalReceiverComponent? receiver) ||
                args.Session.AttachedEntity is not EntityUid attached || attached == default ||
                !TryComp(attached, out ActorComponent? actor))
                return;

            if (mappings.TryFirstOrDefault(map => !map.ExceptBy(transmitter.Outputs.Keys, item => item.Item1).Any() &&
                !map.ExceptBy(receiver.Inputs.Keys, item => item.Item2).Any(), out var mapping))
            {
                foreach (var (port, receivers) in transmitter.Outputs)
                    if (receivers.RemoveAll(id => id.uid == receiver.Owner) > 0)
                        RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(port));

                foreach (var (port, transmitters) in receiver.Inputs)
                    if (transmitters.RemoveAll(id => id.uid == transmitter.Owner) > 0)
                        RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(port));

                foreach (var (t, r) in mapping)
                    TryLink(transmitter, receiver, new(t, r));
            }

            TryUpdateUI(linker, transmitter, receiver);
        }

        private void OnLinkerUIClosed(EntityUid uid, SignalLinkerComponent component, BoundUIClosedEvent args)
        {
            component.savedTransmitter = null;
            component.savedReceiver = null;
        }

        private bool IsInRange(SignalTransmitterComponent transmitterComponent, SignalReceiverComponent receiverComponent)
        {
            if (TryComp(transmitterComponent.Owner, out ApcPowerReceiverComponent? transmitterPowerReceiverComponent) &&
                TryComp(receiverComponent.Owner, out ApcPowerReceiverComponent? receiverPowerReceiverComponent)
                ) // TODO && are they on the same powernet?
                return true;

            return Comp<TransformComponent>(transmitterComponent.Owner).MapPosition.InRange(
                   Comp<TransformComponent>(receiverComponent.Owner).MapPosition, 30f); // TODO should this be a constant?
        }
    }
}
