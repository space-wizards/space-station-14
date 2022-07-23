using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    sealed class SetGamePresetCommand : IConsoleCommand
    {
        public string Command => "setgamepreset";
        public string Description => Loc.GetString("set-game-preset-command-description", ("command", Command));
        public string Help => Loc.GetString("set-game-preset-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 1), ("currentAmount", args.Length)));
                return;
            }

            var ticker = EntitySystem.Get<GameTicker>();

            if (!ticker.TryFindGamePreset(args[0], out var preset))
            {
                shell.WriteError(Loc.GetString("set-game-preset-preset-error", ("preset", args[0])));
                return;
            }

            ticker.SetGamePreset(preset);
            shell.WriteLine(Loc.GetString("set-game-preset-preset-set", ("preset", preset.ID)));
        }
    }
}
