using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking.Presets;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class SetGamePresetCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entity = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

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

            var ticker = _entity.System<GameTicker>();

            if (!ticker.TryFindGamePreset(args[0], out var preset))
            {
                shell.WriteError(Loc.GetString("set-game-preset-preset-error", ("preset", args[0])));
                return;
            }

            ticker.SetGamePreset(preset);
            shell.WriteLine(Loc.GetString("set-game-preset-preset-set", ("preset", preset.ID)));
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var gamePresets = _prototype.EnumeratePrototypes<GamePresetPrototype>()
                    .OrderBy(p => p.ID);
                var options = new List<string>();
                foreach (var preset in gamePresets)
                {
                    options.Add(preset.ID);
                    options.AddRange(preset.Alias);
                }

                return CompletionResult.FromHintOptions(options, "<id>");
            }
            return CompletionResult.Empty;
        }
    }
}
