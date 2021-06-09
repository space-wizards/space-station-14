using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Server)]
    class SetGamePresetCommand : IConsoleCommand
    {
        public string Command => "setgamepreset";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Need exactly one argument.");
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();

            ticker.SetStartPreset(args[0]);
        }
    }
}
