using Content.Server.Administration;
using Content.Server.GameTicking.Presets;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class GoLobbyCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _e = default!;

        public string Command => "golobby";
        public string Description => "Enables the lobby and restarts the round.";
        public string Help => $"Usage: {Command} / {Command} <preset>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            GamePresetPrototype? preset = null;
            var presetName = string.Join(" ", args);

            var ticker = _e.System<GameTicker>();

            if (args.Length > 0)
            {
                if (!ticker.TryFindGamePreset(presetName, out preset))
                {
                    shell.WriteLine($"No preset found with name {presetName}");
                    return;
                }
            }

            var config = IoCManager.Resolve<IConfigurationManager>();
            config.SetCVar(CCVars.GameLobbyEnabled, true);

            ticker.RestartRound();

            if (preset != null)
            {
                ticker.SetGamePreset(preset);
            }

            shell.WriteLine($"Enabling the lobby and restarting the round.{(preset == null ? "" : $"\nPreset set to {presetName}")}");
        }
    }
}
