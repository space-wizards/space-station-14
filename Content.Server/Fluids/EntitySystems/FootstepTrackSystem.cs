using Content.Server.Fluids.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;

namespace Content.Server.Fluids.EntitySystems;

/// <summary>
/// Creates decals when ending collision with the attached entity.
/// </summary>
public sealed class FootstepTrackSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FootstepTrackComponent, EndCollideEvent>(OnFootstepEndCollide);
    }

    // TODO: Need a tracking component, stop tracking after N distance from source
    // TODO: Make the trails cleanable
    // TODO: If puddle spawns on tile then dump the tracks.
    // TODO: Bump puddle volume

    private void OnFootstepEndCollide(EntityUid uid, FootstepTrackComponent component, ref EndCollideEvent args)
    {
        // TODO: Do footstep tracking
        if (!args.OtherFixture.Body.Hard)
        {
            return;
        }

        var xform = Transform(uid);

        if (!xform.Anchored)
            return;

        var otherXform = Transform(uid);

        if (!_mapManager.TryGetGrid(otherXform.GridUid, out var grid) ||
            !grid.TryGetTileRef(otherXform.Coordinates, out var tileBRef) ||
            !grid.TryGetTileRef(xform.Coordinates, out var tileARef))
        {
            return;
        }

        var direction = tileBRef.GridIndices - tileARef.GridIndices;
    }
}
