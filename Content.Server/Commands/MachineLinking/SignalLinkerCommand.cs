using Content.Server.Administration;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands.MachineLinking
{
    [AdminCommand(AdminFlags.Debug)]
    public class SignalLinkerCommand : IConsoleCommand
    {
        public string Command => "signallink";

        public string Description => "Turns on signal linker mode. Click a transmitter to tune that signal and then click on each receiver to tune them to the transmitter signal.";

        public string Help => "signallink (on/off)";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
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
            shell.WriteLine(ret ? "Enabled" : "Disabled");
        }
    }
}
