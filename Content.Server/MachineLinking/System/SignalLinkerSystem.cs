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
    public class SignalLinkerSystem : EntitySystem
    {
        private InteractionSystem _interaction = default!;

        private SignalLinkCollection _linkCollection = new();

        public override void Initialize()
        {
            base.Initialize();

            _interaction = Get<InteractionSystem>();

            SubscribeLocalEvent<SignalTransmitterComponent, ComponentStartup>(TransmitterStartupHandler);
            SubscribeLocalEvent<SignalTransmitterComponent, InteractUsingEvent>(TransmitterInteractUsingHandler);
            SubscribeLocalEvent<SignalTransmitterComponent, InvokePortEvent>(OnTransmitterInvokePort);

            SubscribeLocalEvent<SignalReceiverComponent, ComponentStartup>(OnReceiverStartup);
            SubscribeLocalEvent<SignalReceiverComponent, InteractUsingEvent>(OnReceiverInteractUsing);

            SubscribeLocalEvent<SignalReceiverComponent, ComponentRemove>(OnReceiverRemoved);
            SubscribeLocalEvent<SignalTransmitterComponent, ComponentRemove>(OnTransmitterRemoved);
        }

        private void OnTransmitterRemoved(EntityUid uid, SignalTransmitterComponent component, ComponentRemove args)
        {
            _linkCollection.RemoveLinks(component);
        }

        private void OnReceiverRemoved(EntityUid uid, SignalReceiverComponent component, ComponentRemove args)
        {
            _linkCollection.RemoveLinks(component);
        }

        private void OnTransmitterInvokePort(EntityUid uid, SignalTransmitterComponent component, InvokePortEvent args)
        {
            if (!component.Outputs.TryGetPort(args.Port, out var port)) throw new PortNotFoundException();

            if (args.Value == null)
            {
                if (port.Type != null && !port.Type.IsNullable()) throw new InvalidPortValueException();
            }
            else
            {
                if (port.Type == null || !args.Value.GetType().IsAssignableTo(port.Type))
                    throw new InvalidPortValueException();
            }

            port.Signal = args.Value;

            foreach (var link in _linkCollection.GetLinks(component, port.Name))
            {
                if (!IsInRange(component, link.ReceiverComponent)) continue;

                RaiseLocalEvent(link.ReceiverComponent.Owner.Uid,
                    new SignalReceivedEvent(link.Receiverport.Name, args.Value), false);
            }
        }

        private void OnReceiverInteractUsing(EntityUid uid, SignalReceiverComponent component, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!args.Used.TryGetComponent<SignalLinkerComponent>(out var linker) || !linker.Port.HasValue ||
                !args.User.TryGetComponent(out ActorComponent? actor) ||
                !linker.Port.Value.transmitter.Outputs.TryGetPort(linker.Port.Value.port, out var port))
            {
                return;
            }

            if (component.Inputs.Count == 1)
            {
                LinkerInteraction(args.User, linker.Port.Value.transmitter, linker.Port.Value.port, component,
                    component.Inputs[0].Name);
                args.Handled = true;
                return;
            }

            var bui = component.Owner.GetUIOrNull(SignalReceiverUiKey.Key);
            if (bui == null)
            {
                return;
            }

            bui.Open(actor.PlayerSession);
            bui.SetState(
                new SignalPortsState(new Dictionary<string, bool>(component.Inputs.GetValidatedPorts(port.Type))));
            args.Handled = true;
        }

        private void OnReceiverStartup(EntityUid uid, SignalReceiverComponent component, ComponentStartup args)
        {
            if (component.Owner.GetUIOrNull(SignalReceiverUiKey.Key) is { } ui)
                ui.OnReceiveMessage += msg => OnReceiverUIMessage(uid, component, msg);
        }

        private void OnReceiverUIMessage(EntityUid uid, SignalReceiverComponent component,
            ServerBoundUserInterfaceMessage msg)
        {
            switch (msg.Message)
            {
                case SignalPortSelected portSelected:
                    if (msg.Session.AttachedEntity == null ||
                        !msg.Session.AttachedEntity.TryGetComponent(out HandsComponent? hands) ||
                        !hands.TryGetActiveHeldEntity(out var heldEntity) ||
                        !heldEntity.TryGetComponent(out SignalLinkerComponent? signalLinkerComponent) ||
                        !_interaction.InRangeUnobstructed(msg.Session.AttachedEntity, component.Owner, ignoreInsideBlocker: true) ||
                        !signalLinkerComponent.Port.HasValue ||
                        !signalLinkerComponent.Port.Value.transmitter.Outputs.ContainsPort(signalLinkerComponent.Port
                            .Value.port) || !component.Inputs.ContainsPort(portSelected.Port))
                        return;
                    LinkerInteraction(msg.Session.AttachedEntity, signalLinkerComponent.Port.Value.transmitter,
                        signalLinkerComponent.Port.Value.port, component, portSelected.Port);
                    break;
            }
        }

        private void TransmitterStartupHandler(EntityUid uid, SignalTransmitterComponent component,
            ComponentStartup args)
        {
            if (component.Owner.GetUIOrNull(SignalTransmitterUiKey.Key) is { } ui)
                ui.OnReceiveMessage += msg => OnTransmitterUIMessage(uid, component, msg);

            foreach (var portPrototype in component.Outputs)
            {
                if (portPrototype.Type == null) continue;

                var valueRequest = new SignalValueRequestedEvent(portPrototype.Name, portPrototype.Type);
                RaiseLocalEvent(uid, valueRequest, false);

                if (!valueRequest.Handled) throw new NoSignalValueProvidedException();

                portPrototype.Signal = valueRequest.Signal;
            }
        }

        private void OnTransmitterUIMessage(EntityUid uid, SignalTransmitterComponent component,
            ServerBoundUserInterfaceMessage msg)
        {
            switch (msg.Message)
            {
                case SignalPortSelected portSelected:
                    if (msg.Session.AttachedEntity == null ||
                        !msg.Session.AttachedEntity.TryGetComponent(out HandsComponent? hands) ||
                        !hands.TryGetActiveHeldEntity(out var heldEntity) ||
                        !heldEntity.TryGetComponent(out SignalLinkerComponent? signalLinkerComponent) ||
                        !_interaction.InRangeUnobstructed(msg.Session.AttachedEntity, component.Owner, ignoreInsideBlocker: true))
                        return;
                    LinkerSaveInteraction(msg.Session.AttachedEntity, signalLinkerComponent, component,
                        portSelected.Port);
                    break;
            }
        }

        private void TransmitterInteractUsingHandler(EntityUid uid, SignalTransmitterComponent component,
            InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!args.Used.TryGetComponent<SignalLinkerComponent>(out var linker) ||
                !args.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            if (component.Outputs.Count == 1)
            {
                var port = component.Outputs.First();
                LinkerSaveInteraction(args.User, linker, component, port.Name);
                args.Handled = true;
                return;
            }

            var bui = component.Owner.GetUIOrNull(SignalTransmitterUiKey.Key);
            if (bui == null) return;
            bui.Open(actor.PlayerSession);
            bui.SetState(new SignalPortsState(component.Outputs.GetPortStrings().ToArray()));
            args.Handled = true;
        }

        private void LinkerInteraction(IEntity entity, SignalTransmitterComponent transmitter, string transmitterPort,
            SignalReceiverComponent receiver, string receiverPort)
        {
            if (_linkCollection.LinkExists(transmitter, transmitterPort, receiver, receiverPort))
            {
                if (_linkCollection.RemoveLink(transmitter, transmitterPort, receiver, receiverPort))
                {
                    RaiseLocalEvent(receiver.Owner.Uid, new PortDisconnectedEvent(receiverPort));
                    RaiseLocalEvent(transmitter.Owner.Uid, new PortDisconnectedEvent(transmitterPort));
                    entity.PopupMessageCursor(Loc.GetString("signal-linker-component-unlinked-port",
                        ("port", receiverPort), ("machine", receiver)));
                }
            }
            else
            {
                var tport = transmitter.Outputs.GetPort(transmitterPort);
                var rport = receiver.Inputs.GetPort(receiverPort);

                if (!IsInRange(transmitter, receiver))
                {
                    entity.PopupMessageCursor(Loc.GetString("signal-linker-component-out-of-range"));
                    return;
                }

                if (tport.MaxConnections != 0 && tport.MaxConnections >= _linkCollection.LinkCount(transmitter))
                {
                    entity.PopupMessageCursor(Loc.GetString("signal-linker-component-max-connections-transmitter"));
                    return;
                }

                if (rport.MaxConnections != 0 && rport.MaxConnections <= _linkCollection.LinkCount(receiver))
                {
                    entity.PopupMessageCursor(Loc.GetString("signal-linker-component-max-connections-receiver"));
                    return;
                }

                if (tport.Type != rport.Type)
                {
                    entity.PopupMessageCursor(Loc.GetString("signal-linker-component-type-mismatch"));
                    return;
                }

                var linkAttempt = new LinkAttemptEvent(entity, transmitter, transmitterPort, receiver, receiverPort);
                RaiseLocalEvent(receiver.Owner.Uid, linkAttempt);
                RaiseLocalEvent(transmitter.Owner.Uid, linkAttempt);

                if (linkAttempt.Cancelled) return;

                var link = _linkCollection.AddLink(transmitter, transmitterPort, receiver, receiverPort);
                if (link.Transmitterport.Signal != null)
                    RaiseLocalEvent(receiver.Owner.Uid,
                        new SignalReceivedEvent(receiverPort, link.Transmitterport.Signal));

                entity.PopupMessageCursor(Loc.GetString("signal-linker-component-linked-port", ("port", receiverPort),
                    ("machine", receiver)));
            }
        }

        private void LinkerSaveInteraction(IEntity entity, SignalLinkerComponent linkerComponent,
            SignalTransmitterComponent transmitterComponent, string transmitterPort)
        {
            if (SavePortInSignalLinker(linkerComponent, transmitterComponent, transmitterPort))
            {
                entity.PopupMessageCursor(Loc.GetString("signal-linker-component-saved-port", ("port", transmitterPort),
                    ("machine", transmitterComponent.Owner)));
            }
        }

        private bool SavePortInSignalLinker(SignalLinkerComponent linker, SignalTransmitterComponent transmitter,
            string port)
        {
            if (!transmitter.Outputs.ContainsPort(port)) return false;
            linker.Port = (transmitter, port);
            return true;
        }

        private bool IsInRange(SignalTransmitterComponent transmitterComponent,
            SignalReceiverComponent receiverComponent)
        {
            if (transmitterComponent.Owner.TryGetComponent<ApcPowerReceiverComponent>(
                    out var transmitterPowerReceiverComponent) &&
                receiverComponent.Owner.TryGetComponent<ApcPowerReceiverComponent>(
                    out var receiverPowerReceiverComponent)
            ) //&& todo are they on the same powernet?
            {
                return true;
            }

            return transmitterComponent.Owner.Transform.MapPosition.InRange(
                receiverComponent.Owner.Transform.MapPosition, 30f);
        }
    }
}
