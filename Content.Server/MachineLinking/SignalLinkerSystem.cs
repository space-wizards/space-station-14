using System.Collections.Generic;
using System.Linq;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.MachineLinking.Components;
using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;
using Content.Shared.Notification.Managers;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.MachineLinking
{
    public class SignalLinkerSystem : EntitySystem
    {
        [Dependency] private IComponentManager _componentManager = default!;
        private InteractionSystem _interaction = default!;

        public override void Initialize()
        {
            base.Initialize();

            _interaction = Get<InteractionSystem>();

            SubscribeLocalEvent<SignalTransmitterComponent, ComponentStartup>(TransmitterStartupHandler);
            SubscribeLocalEvent<SignalTransmitterComponent, InteractUsingEvent>(TransmitterInteractUsingHandler);

            SubscribeLocalEvent<SignalReceiverComponent, ComponentStartup>(OnReceiverStartup);
            SubscribeLocalEvent<SignalReceiverComponent, InteractUsingEvent>(OnReceiverInteractUsing);
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
            throw new System.NotImplementedException();
        }

        private void TransmitterStartupHandler(EntityUid uid, SignalTransmitterComponent component, ComponentStartup args)
        {
            if(component.Owner.GetUIOrNull(SignalTransmitterUiKey.Key) is {} ui)
                ui.OnReceiveMessage += msg => OnTransmitterUIMessage(uid, component, msg);
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
