using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.Commands.GameTicking
{
    [AdminCommand(AdminFlags.Server)]
    class ForcePresetCommand : IConsoleCommand
    {
        public string Command => "forcepreset";
        public string Description => "Forces a specific game preset to start for the current lobby.";
        public string Help => $"Usage: {Command} <preset>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();
            if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine("This can only be executed while the game is in the pre-round lobby.");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine("Need exactly one argument.");
                return;
            }

            var name = args[0];
            if (!ticker.TryGetPreset(name, out var type))
            {
                shell.WriteLine($"No preset exists with name {name}.");
                return;
            }

            ticker.SetStartPreset(type, true);
            shell.WriteLine($"Forced the game to start with preset {name}.");
        }
    }
}
