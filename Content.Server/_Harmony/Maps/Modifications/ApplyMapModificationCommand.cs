using System.Linq;
using Content.Server._Harmony.Maps.Modifications.Systems;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Harmony.Maps.Modifications;

[AdminCommand(AdminFlags.Fun)] // I'm not sure if this is the right flag but it should be fine
public sealed class ApplyMapModificationCommand : LocalizedCommands
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Command => "applymapmodification";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var mapModificationSystem = _entityManager.System<MapModificationSystem>();

        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!_prototypeManager.TryIndex<MapModificationPrototype>(args[0], out var modification))
        {
            shell.WriteLine(Loc.GetString("cmd-applymapmodification-modification-not-found", ("modification", args[0])));
            return;
        }

        if (!int.TryParse(args[1], out var intGridId))
        {
            shell.WriteLine(Loc.GetString("cmd-applymapmodification-failure-integer", ("arg", args[1])));
            return;
        }

        var grid = new EntityUid(intGridId);

        if (!_entityManager.EntityExists(grid))
        {
            shell.WriteLine(Loc.GetString("cmd-applymapmodification-grid-not-found", ("grid", intGridId)));
            return;
        }

        if (shell.Player is { } player)
        {
            _adminLogger.Add(
                LogType.AdminCommands,
                LogImpact.Extreme,
                $"Player {player.Name} ({player.UserId}) applied map modification {modification.ID} to grid {_entityManager.ToPrettyString(grid):grid}.");
        }
        else
        {
            _adminLogger.Add(
                LogType.AdminCommands,
                LogImpact.Extreme,
                $"Map modification {modification.ID} was applied to grid {_entityManager.ToPrettyString(grid):grid}.");
        }

        mapModificationSystem.ApplyMapModification(modification, grid);

        shell.WriteLine(Loc.GetString("cmd-applymapmodification-success", ("modification", modification.ID), ("grid", _entityManager.ToPrettyString(grid))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                var options = IoCManager.Resolve<IPrototypeManager>()
                    .EnumeratePrototypes<MapModificationPrototype>()
                    .Select(p => new CompletionOption(p.ID))
                    .OrderBy(p => p.Value);

                return CompletionResult.FromOptions(options);
            case 2:
                var mapGrids = _entityManager.EntityQueryEnumerator<MetaDataComponent, MapGridComponent>();
                var allGrids = new List<CompletionOption>();
                while (mapGrids.MoveNext(out var entity, out var metadata, out var _))
                {
                    allGrids.Add(new CompletionOption(entity.ToString(), metadata.EntityName));
                }

                return CompletionResult.FromHintOptions(allGrids, Loc.GetString("cmd-applymapmodification-grid-hint"));
        }
        return CompletionResult.Empty;
    }
}
