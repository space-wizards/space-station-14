using Content.Server.Atmos.Piping.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Generation.Teg;

public sealed class TegSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TegGeneratorComponent, AtmosDeviceUpdateEvent>(GeneratorUpdate);

        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    private void GeneratorUpdate(EntityUid uid, TegGeneratorComponent component, AtmosDeviceUpdateEvent args)
    {
        // Find circulators next to the TEG center.
        // TODO: Track these persistently rather than immediately.

        if (!_xformQuery.TryGetComponent(uid, out var xform) || !xform.Anchored)
            return;

        var gridUid = xform.GridUid;
        if (gridUid == null || gridUid != xform.ParentUid || !TryComp(gridUid, out MapGridComponent? grid))
            return;

        var pos = _map.TileIndicesFor(gridUid.Value, grid, xform.Coordinates);
    }


}
