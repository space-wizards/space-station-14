using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking.Presets;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class SetGamePresetCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly GameTicker _gameTicker = default!;

        public override string Command => "setgamepreset";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is < 1 or > 3)
            {
                shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 1), ("upper", 3), ("currentAmount", args.Length)));
                return;
            }

            if (!_gameTicker.TryFindGamePreset(args[0], out var preset))
            {
                shell.WriteError(Loc.GetString("cmd-setgamepreset-preset-error", ("preset", args[0])));
                return;
            }

            var rounds = 1;

            if (args.Length >= 2 && !int.TryParse(args[1], out rounds))
            {
                shell.WriteError(Loc.GetString("cmd-setgamepreset-optional-argument-not-integer"));
                return;
            }

            GamePresetPrototype? decoy = null;

            if (args.Length == 3 && !_gameTicker.TryFindGamePreset(args[2], out decoy))
            {
                shell.WriteError(Loc.GetString("cmd-setgamepreset-decoy-error", ("preset", args[2])));
                return;
            }

            _gameTicker.SetGamePreset(preset, false, decoy, rounds);
            if (decoy == null)
                shell.WriteLine(Loc.GetString("cmd-setgamepreset-preset-set-finite", ("preset", preset.ID), ("rounds", rounds.ToString())));
            else
                shell.WriteLine(Loc.GetString("cmd-setgamepreset-preset-set-finite-with-decoy", ("preset", preset.ID), ("rounds", rounds.ToString()), ("decoy", decoy.ID)));
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<GamePresetPrototype>(),
                Loc.GetString("cmd-setgamepreset-hint-1")),
                2 => CompletionResult.FromHint(Loc.GetString("cmd-setgamepreset-hint-2")),
                3 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<GamePresetPrototype>(),
                Loc.GetString("cmd-setgamepreset-hint-3")),

                _ => CompletionResult.Empty
            };
        }
    }
}
