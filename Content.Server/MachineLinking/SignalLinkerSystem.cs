using System;
using System.Collections.Generic;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.MachineLinking.Components;
using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Server.MachineLinking
{
    public class SignalLinkerSystem : EntitySystem
    {
        [Dependency] private ComponentManager _componentManager = default!;
        private InteractionSystem _interaction = default!;

        public override void Initialize()
        {
            base.Initialize();

            _interaction = Get<InteractionSystem>();

            SubscribeLocalEvent<SignalTransmitterComponent, ComponentStartup>(TransmitterStartupHandler);
            SubscribeLocalEvent<SignalTransmitterComponent, InteractUsingEvent>(TransmitterInteractUsingHandler);
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
                case SignalTransmitterPortSelected portSelected:
                    if (msg.Session.AttachedEntity == null ||
                        !msg.Session.AttachedEntity.TryGetComponent(out HandsComponent? hands) ||
                        !hands.TryGetActiveHeldEntity(out var heldEntity) ||
                        !heldEntity.TryGetComponent(out SignalLinkerComponent? signalLinkerComponent) ||
                        !component.Outputs.ContainsKey(portSelected.Port) ||
                        !_interaction.InRangeUnobstructed(msg.Session.AttachedEntity, component.Owner))
                        return;
                    signalLinkerComponent.Port = (component, portSelected.Port);
                    break;
            }
        }

        private void TransmitterInteractUsingHandler(EntityUid uid, SignalTransmitterComponent component, InteractUsingEvent args)
        {
            if (!_componentManager.HasComponent<SignalLinkerComponent>(uid) || !args.User.TryGetComponent(out ActorComponent? actor))
                return;

            component.Owner.GetUIOrNull(SignalTransmitterUiKey.Key)?.Open(actor.PlayerSession);
        }

        //todo paul oldcode

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
        }
    }
}
