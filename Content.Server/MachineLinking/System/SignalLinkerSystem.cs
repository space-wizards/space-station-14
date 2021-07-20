using System.Collections.Generic;
using System.Linq;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Exceptions;
using Content.Server.MachineLinking.Models;
using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;
using Content.Shared.Notification.Managers;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.MachineLinking.System
{
    public class SignalLinkerSystem : EntitySystem
    {
        [Dependency] private IComponentManager _componentManager = default!;
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
            if (!component.Outputs.TryGetPort(args.Port, out var port))
                throw new PortNotFoundException();

            if (args.Value == null)
            {
                if (port.Type == null || !port.Type.IsNullable())
                    throw new InvalidPortValueException();
            }
            else
            {
                if (port.Type == null || args.Value.GetType().IsAssignableTo(port.Type))
                    throw new InvalidPortValueException();
            }

            port.Signal = args.Value;

            foreach (var link in _linkCollection.GetLinks(component, port.Name))
            {
                RaiseLocalEvent(link.ReceiverComponent.Owner.Uid, new SignalReceivedEvent(link.Receiverport.Name, args.Value));
            }
        }

        private void OnReceiverInteractUsing(EntityUid uid, SignalReceiverComponent component, InteractUsingEvent args)
        {
            if (!args.Used.TryGetComponent<SignalLinkerComponent>(out var linker) || !linker.Port.HasValue || !args.User.TryGetComponent(out ActorComponent? actor) || !linker.Port.Value.transmitter.Outputs.TryGetPort(linker.Port.Value.port, out var port))
                return;

            var bui = component.Owner.GetUIOrNull(SignalReceiverUiKey.Key);
            if (bui == null) return;

            bui.Open(actor.PlayerSession);
            bui.SetState(new SignalPortsState(new Dictionary<string, bool>(component.Inputs.GetValidatedPorts(port.Type))));
        }

        private void OnReceiverStartup(EntityUid uid, SignalReceiverComponent component, ComponentStartup args)
        {
            if(component.Owner.GetUIOrNull(SignalReceiverUiKey.Key) is {} ui)
                ui.OnReceiveMessage += msg => OnReceiverUIMessage(uid, component, msg);
        }

        private void OnReceiverUIMessage(EntityUid uid, SignalReceiverComponent component, ServerBoundUserInterfaceMessage msg)
        {
            switch (msg.Message)
            {
                case SignalPortSelected portSelected:
                    if (msg.Session.AttachedEntity == null ||
                        !msg.Session.AttachedEntity.TryGetComponent(out HandsComponent? hands) ||
                        !hands.TryGetActiveHeldEntity(out var heldEntity) ||
                        !heldEntity.TryGetComponent(out SignalLinkerComponent? signalLinkerComponent) ||
                        !_interaction.InRangeUnobstructed(msg.Session.AttachedEntity, component.Owner) ||
                        !signalLinkerComponent.Port.HasValue ||
                        !signalLinkerComponent.Port.Value.transmitter.Outputs.ContainsPort(signalLinkerComponent.Port.Value.port) ||
                        !component.Inputs.ContainsPort(portSelected.Port))
                        return;
                    Connect(signalLinkerComponent.Port.Value.transmitter, signalLinkerComponent.Port.Value.port, component, portSelected.Port);
                    break;
            }
        }

        private void TransmitterStartupHandler(EntityUid uid, SignalTransmitterComponent component, ComponentStartup args)
        {
            if(component.Owner.GetUIOrNull(SignalTransmitterUiKey.Key) is {} ui)
                ui.OnReceiveMessage += msg => OnTransmitterUIMessage(uid, component, msg);

            foreach (var portPrototype in component.Outputs)
            {
                if (portPrototype.Type == null)
                    continue;

                var valueRequest = new SignalValueRequestedEvent(portPrototype.Name, portPrototype.Type);
                RaiseLocalEvent(uid, valueRequest, false);

                if (!valueRequest.Handled)
                    throw new NoSignalValueProvidedException();

                portPrototype.Signal = valueRequest.Signal;
            }
        }

        private void OnTransmitterUIMessage(EntityUid uid, SignalTransmitterComponent component, ServerBoundUserInterfaceMessage msg)
        {
            switch (msg.Message)
            {
                case SignalPortSelected portSelected:
                    if (msg.Session.AttachedEntity == null ||
                        !msg.Session.AttachedEntity.TryGetComponent(out HandsComponent? hands) ||
                        !hands.TryGetActiveHeldEntity(out var heldEntity) ||
                        !heldEntity.TryGetComponent(out SignalLinkerComponent? signalLinkerComponent) ||
                        !_interaction.InRangeUnobstructed(msg.Session.AttachedEntity, component.Owner))
                        return;
                    if (SavePortInSignalLinker(signalLinkerComponent, component, portSelected.Port))
                    {
                        msg.Session.AttachedEntity?.PopupMessageCursor(Loc.GetString("signal-linker-component-saved-port",
                            ("port", portSelected.Port),
                            ("machine", component.Owner)));
                    }
                    break;
            }
        }

        private bool SavePortInSignalLinker(SignalLinkerComponent linker, SignalTransmitterComponent transmitter,
            string port)
        {
            if (!transmitter.Outputs.ContainsPort(port)) return false;
            linker.Port = (transmitter, port);
            return true;
        }

        private void TransmitterInteractUsingHandler(EntityUid uid, SignalTransmitterComponent component, InteractUsingEvent args)
        {
            if (!args.Used.TryGetComponent<SignalLinkerComponent>(out var linker) || !args.User.TryGetComponent(out ActorComponent? actor))
                return;

            if(component.Outputs.Count == 1)
            {
                var port = component.Outputs.First();
                if (SavePortInSignalLinker(linker, component, port.Name))
                {
                    args.User.PopupMessageCursor(Loc.GetString("signal-linker-component-saved-port",
                        ("port", port.Name),
                        ("machine", component.Owner)));
                }
                return;
            }

            var bui = component.Owner.GetUIOrNull(SignalTransmitterUiKey.Key);
            if (bui == null) return;
            bui.Open(actor.PlayerSession);
            bui.SetState(new SignalPortsState(component.Outputs.GetPortStrings().ToArray()));
        }

        private void Connect(SignalTransmitterComponent transmitter, string transmitterPort,
            SignalReceiverComponent receiver, string receiverPort)
        {
            if (transmitter.Outputs.TryGetPort(transmitterPort, out var tport) && tport.MaxConnections != 0 &&
                tport.MaxConnections >= _linkCollection.LinkCount(transmitter))
            {
                return;
            }

            if (receiver.Inputs.TryGetPort(receiverPort, out var rport) && rport.MaxConnections != 0 &&
                rport.MaxConnections >= _linkCollection.LinkCount(receiver))
            {
                return;
            }

            _linkCollection.AddLink(transmitter, transmitterPort, receiver, receiverPort);
        }

        private void Disconnect(SignalTransmitterComponent transmitter, string transmitterPort,
            SignalReceiverComponent receiver, string receiverPort)
        {
            _linkCollection.RemoveLink(transmitter, transmitterPort, receiver, receiverPort);
        }

        /*todo paul oldcode

        private readonly Dictionary<NetUserId, SignalTransmitterComponent?> _transmitters = new();

        public bool SignalLinkerKeybind(NetUserId id, bool? enable)
        {
            enable ??= !_transmitters.ContainsKey(id);

            if (enable.Value)
            {
                if (_transmitters.ContainsKey(id))
                {
                    return true;
                }

                if (_transmitters.Count == 0)
                {
                    CommandBinds.Builder
                        .BindBefore(EngineKeyFunctions.Use,
                            new PointerInputCmdHandler(HandleUse),
                            typeof(InteractionSystem))
                        .Register<SignalLinkerSystem>();
                }

                _transmitters.Add(id, null);

            }
            else
            {
                if (!_transmitters.ContainsKey(id))
                {
                    return false;
                }

                _transmitters.Remove(id);
                if (_transmitters.Count == 0)
                {
                    CommandBinds.Unregister<SignalLinkerSystem>();
                }
            }

            return enable.Value;
        }

        private bool HandleUse(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (session?.AttachedEntity == null)
            {
                return false;
            }

            if (!_transmitters.TryGetValue(session.UserId, out var signalTransmitter))
            {
                return false;
            }

            if (!EntityManager.TryGetEntity(uid, out var entity))
            {
                return false;
            }

            if (entity.TryGetComponent<SignalReceiverComponent>(out var signalReceiver))
            {
                if (signalReceiver.Interact(session.AttachedEntity, signalTransmitter))
                {
                    return true;
                }
            }

            if (entity.TryGetComponent<SignalTransmitterComponent>(out var transmitter))
            {
                _transmitters[session.UserId] = transmitter.GetSignal(session.AttachedEntity);

                return true;
            }

            return false;
        }*/
    }
}
