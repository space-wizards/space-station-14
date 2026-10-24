using Content.Server.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Map.Components;

namespace Content.Server.Light.EntitySystems;

/// <inheritdoc/>
public sealed partial class RoofSystem : SharedRoofSystem
{
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SetRoofComponent, ComponentStartup>(OnFlagStartup);
    }

    private void OnFlagStartup(Entity<SetRoofComponent> ent, ref ComponentStartup args)
    {
        var xform = Transform(ent.Owner);

        if (_mapGridQuery.TryComp(xform.GridUid, out var grid))
        {
            var index = _maps.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);
            SetRoof((xform.GridUid.Value, grid, null), index, ent.Comp.Value);
        }

        QueueDel(ent.Owner);
    }
}
