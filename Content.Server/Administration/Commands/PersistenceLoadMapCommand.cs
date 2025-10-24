using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PersistenceLoadMap : LocalizedEntityCommands
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override string Command => "persistenceloadmap";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var path = args[0];

        var loadId = new ResPath(path);
        bool save_stat = _mapLoader.TryLoadMap(loadId, out var entity, out var grids);
        shell.WriteLine(Loc.GetString("Did the thing load? ") + $"{save_stat}" + $"{entity}");
        var player = shell.Player;

        if (player == null)
        {
            shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (player.AttachedEntity == null)
        {
            shell.WriteLine(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        EntityUid pe = player.AttachedEntity.Value;
        var coords = _entManager.GetComponent<TransformComponent>(pe).Coordinates;
        if (entity != null)
        {
            _transform.SetCoordinates(entity.Value, coords);
        }

        
        
    }
}
