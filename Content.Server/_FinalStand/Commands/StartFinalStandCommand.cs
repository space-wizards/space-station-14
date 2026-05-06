using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server._FinalStand.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class StartFinalStandCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameMapManager _mapManager = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    public override string Command => "startfinalstand";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_ticker.TryFindGamePreset("finalstand", out var preset))
        {
            shell.WriteError("FinalStand preset not found. Is the prototype loaded?");
            return;
        }

        if (!_mapManager.CheckMapExists("FinalStandMap"))
        {
            shell.WriteError("FinalStandMap not found. Is the prototype loaded?");
            return;
        }

        _cfg.SetCVar(CCVars.GameLobbyEnabled, true);
        _cfg.SetCVar(CCVars.GameMap, "FinalStandMap");
        _ticker.SetGamePreset(preset, true);
        _ticker.RestartRound();

        shell.WriteLine("Final Stand round starting on FinalStandMap.");
    }
}
