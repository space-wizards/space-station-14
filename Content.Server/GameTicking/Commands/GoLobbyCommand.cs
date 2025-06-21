using Content.Server.Administration;
using Content.Server.GameTicking.Presets;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class GoLobbyCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        public override string Command => "golobby";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            GamePresetPrototype? preset = null;
            var presetName = string.Join(" ", args);

            if (args.Length > 0)
            {
                if (!_gameTicker.TryFindGamePreset(presetName, out preset))
                {
                    shell.WriteLine(Loc.GetString($"cmd-forcepreset-no-preset-found", ("preset", presetName)));
                    return;
                }
            }

            _configManager.SetCVar(CCVars.GameLobbyEnabled, true);

            _gameTicker.RestartRound();

            if (preset != null)
                _gameTicker.SetGamePreset(preset);

            shell.WriteLine(Loc.GetString(preset == null ? "cmd-golobby-success" : "cmd-golobby-success-with-preset", ("preset", presetName)));
        }
    }
}
