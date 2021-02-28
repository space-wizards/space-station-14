using System.Collections.Generic;
using Content.Server.GameObjects.Components.MachineLinking;
using Content.Server.GameObjects.EntitySystems.Click;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    public class SignalLinkerSystem : EntitySystem
    {
        private Dictionary<NetUserId, SignalTransmitterComponent> _transmitters;

        public override void Initialize()
        {
            base.Initialize();

            _transmitters = new Dictionary<NetUserId, SignalTransmitterComponent>();
        }

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
                        .BindBefore(EngineKeyFunctions.Use, new PointerInputCmdHandler(HandleUse), typeof(InteractionSystem))
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

        private bool HandleUse(ICommonSession session, EntityCoordinates coords, EntityUid uid)
        {
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
