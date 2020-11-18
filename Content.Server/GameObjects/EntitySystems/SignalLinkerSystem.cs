using System.Collections.Generic;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.MachineLinking;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.Administration;
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
            if (enable == null)
            {
                enable = !_transmitters.ContainsKey(id);
            }

            if (enable == true)
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
            else if (enable == false)
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
            return enable == true;
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

    [AdminCommand(AdminFlags.Debug)]
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

            var ret = system.SignalLinkerKeybind(player.UserId, enable);
            shell.SendText(player, ret ? "Enabled" : "Disabled");
        }
    }
}
