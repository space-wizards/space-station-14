using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking.Presets;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class SetGamePresetCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entity = default!;

        public string Command => "setgamepreset";
        public string Description => Loc.GetString("set-game-preset-command-description", ("command", Command));
        public string Help => Loc.GetString("set-game-preset-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is < 1 or > 3)
            {
                shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 1), ("upper", 3), ("currentAmount", args.Length)));
                return;
            }

            var ticker = _entity.System<GameTicker>();

            if (!ticker.TryFindGamePreset(args[0], out var preset))
            {
                shell.WriteError(Loc.GetString("set-game-preset-preset-error", ("preset", args[0])));
                return;
            }

            var rounds = 1;

            if (args.Length >= 2 && !int.TryParse(args[1], out rounds))
            {
                shell.WriteError(Loc.GetString("set-game-preset-optional-argument-not-integer"));
                return;
            }

            GamePresetPrototype? decoy = null;

            if (args.Length == 3 && !ticker.TryFindGamePreset(args[2], out decoy))
            {
                shell.WriteError(Loc.GetString("set-game-preset-decoy-error", ("preset", args[2])));
                return;
            }

            ticker.SetGamePreset(preset, false, decoy, rounds);
            if (decoy == null)
                shell.WriteLine(Loc.GetString("set-game-preset-preset-set-finite", ("preset", preset.ID), ("rounds", rounds.ToString())));
            else
                shell.WriteLine(Loc.GetString("set-game-preset-preset-set-finite-with-decoy", ("preset", preset.ID), ("rounds", rounds.ToString()), ("decoy", decoy.ID)));
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<GamePresetPrototype>(),
                Loc.GetString("set-game-preset-command-hint-1")),
                2 => CompletionResult.FromHint(Loc.GetString("set-game-preset-command-hint-2")),
                3 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<GamePresetPrototype>(),
                Loc.GetString("set-game-preset-command-hint-3")),

                _ => CompletionResult.Empty
            };
        }
    }
}
