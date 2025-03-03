using System.Linq;
using System.Globalization;
using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Server.Player;
using Robust.Shared.Console;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.UI;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Tube.Components;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Mind;
using Content.Server.Mind.Commands;
using Content.Server.Prayer;
using Content.Server.Station.Systems;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Configurable;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;
using System.Linq;
using Content.Server.Silicons.Laws;
using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.Player;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Physics.Components;
using static Content.Shared.Configurable.ConfigurationComponent;
using System.Linq;
using Content.Server.Administration.Systems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;



namespace Content.Server.Antag.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListObjectivesCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AdminSystem _adminSystem = default!;

    public override string Command => "lsantags";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var msg = new StringBuilder();

        foreach (var player in _adminSystem.UpdatePlayerList)
        {
            if (player.)

        }

        /*
        // var player = shell.Player;
        // if (player == null || !_players.TryGetSessionByUsername(args[0], out player))
        // {
        //     shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
        //     return;
        // }
        // var minds = _entities.System<SharedMindSystem>();
        // if (!minds.TryGetMind(player, out var mindId, out var mind))
        // {
        //     shell.WriteError(LocalizationManager.GetString("shell-target-entity-does-not-have-message", ("missing", "mind")));
        //     return;
        // }
        // shell.WriteLine($"Objectives for player {player.UserId}:");
        // var objectives = mind.Objectives.ToList();
        // if (objectives.Count == 0)
        // {
        //     shell.WriteLine("None.");
        // }
        // var objectivesSystem = _entities.System<SharedObjectivesSystem>();
        // for (var i = 0; i < objectives.Count; i++)
        // {
        //     var info = objectivesSystem.GetInfo(objectives[i], mindId, mind);
        //     if (info == null)
        //     {
        //         shell.WriteLine($"- [{i}] {objectives[i]} - INVALID");
        //     }
        //     else
        //     {

        //         var progress = (int) (info.Value.Progress * 100f);
        //         shell.WriteLine($"- [{i}] {objectives[i]} ({info.Value.Title}) ({progress}%)");
        //     }
        // }
        */

    }
}
