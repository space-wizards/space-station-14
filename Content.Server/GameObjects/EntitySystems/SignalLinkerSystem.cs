using Content.Server.GameObjects.Components.MachineLinking;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems
{
    public class SignalLinkerSystem : EntitySystem
    {
        private Dictionary<NetSessionId, SignalTransmitterComponent> _transmitters;

        public override void Initialize()
        {
            base.Initialize();

            _transmitters = new Dictionary<NetSessionId, SignalTransmitterComponent>();
        }

        public void SignalLinkerKeybind(NetSessionId id, bool? enable)
        {
            if (enable == null)
            {
                enable = !_transmitters.ContainsKey(id);
            }

            if (enable == true)
            {
                if (_transmitters.ContainsKey(id))
                {
                    return;
                }

                if (_transmitters.Count == 0)
                {
                    CommandBinds.Builder
                        .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(HandleUse))
                        .Register<SignalLinkerSystem>();
                }

                _transmitters.Add(id, null);

            }
            else if (enable == false)
            {
                if (!_transmitters.ContainsKey(id))
                {
                    return;
                }

                _transmitters.Remove(id);
                if (_transmitters.Count == 0)
                {
                    CommandBinds.Unregister<SignalLinkerSystem>();
                }
            }
        }

        private bool HandleUse(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            if (!_transmitters.TryGetValue(session.SessionId, out var signalTransmitter))
            {
                return false;
            }

            if (!EntityManager.TryGetEntity(uid, out var entity))
            {
                return false;
            }

            if (entity == null)
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
                _transmitters[session.SessionId] = transmitter.GetSignal(session.AttachedEntity);

                return true;
            }

            return false;
        }

    }

    public class SignalLinkerCommand : IClientCommand
    {
        public string Command => "signallink";

        public string Description => "Turns on signal linker mode. Click a transmitter to tune that signal and then click on each receiver to tune them to the transmitter signal.";

        public string Help => "signallink (on/off)";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            bool? enable = null;
            if (args.Length > 0)
            {
                if (args[0] == "on")
                    enable = true;
                else if (args[0] == "off")
                    enable = false;
                else if (bool.TryParse(args[0], out var boolean))
                    enable = boolean;
                else if (int.TryParse(args[0], out var num))
                {
                    if (num == 1)
                        enable = true;
                    else if (num == 0)
                        enable = false;
                }
            }

            if (!IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem<SignalLinkerSystem>(out var system))
            {
                return;
            }

            system.SignalLinkerKeybind(player.SessionId, enable);
        }
    }
}
