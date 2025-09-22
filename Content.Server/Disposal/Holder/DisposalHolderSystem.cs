using Content.Server.Atmos.EntitySystems;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using Content.Shared.Maps;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Disposal.Holder;

/// <inheritdoc/>
public sealed partial class DisposalHolderSystem : SharedDisposalHolderSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    /// <inheritdoc/>
    protected override void ExpelAtmos(Entity<DisposalHolderComponent> ent)
    {
        if (_atmos.GetContainingMixture(ent.Owner, false, true) is { } environment)
        {
            _atmos.Merge(environment, ent.Comp.Air);
            ent.Comp.Air.Clear();
        }
    }

    /// <inheritdoc/>
    protected override bool TryEscapingDisposals(Entity<DisposalHolderComponent> ent, Entity<DisposalTubeComponent> tube)
    {
        if (!ent.Comp.TubeVisits.TryGetValue(tube, out var visits))
            return false;

        // Check if the holder should attempt to escape the current pipe
        if (visits > ent.Comp.TubeVisitThreshold &&
            _random.NextFloat() <= ent.Comp.TubeEscapeChance)
        {
            var xform = Transform(tube);

            // Unanchor the pipe
            _xformSystem.Unanchor(tube, xform);

            // Pry up the tile the pipe was under, if applicable
            if (TryComp<MapGridComponent>(xform.GridUid, out var mapGrid))
            {
                var tileRef = _maps.GetTileRef((xform.GridUid.Value, mapGrid), xform.Coordinates);
                _tile.PryTile(tileRef);
            }

            ExitDisposals(ent);

            return true;
        }

        return false;
    }
}
