using System.Numerics;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class XAEThrowThingsAroundSystem : BaseXAESystem<XAEThrowThingsAroundComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEThrowThingsAroundComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var component = ent.Comp;
        var xform = Transform(ent);
        if (TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            var boxForTilesPry = Box2.CenteredAround(_transform.GetWorldPosition(xform), new Vector2(component.Range * 2, component.Range));
            var tiles = _map.GetTilesIntersecting(xform.GridUid.Value, grid, boxForTilesPry, true);

            foreach (var tile in tiles)
            {
                if (!_random.Prob(component.TilePryChance))
                    continue;

                _tile.PryTile(tile);
            }
        }

        var lookup = _lookup.GetEntitiesInRange(ent, component.Range, LookupFlags.Dynamic | LookupFlags.Sundries);
        var physQuery = GetEntityQuery<PhysicsComponent>();
        foreach (var entity in lookup)
        {
            if (physQuery.TryGetComponent(entity, out var phys)
                && (phys.CollisionMask & (int)CollisionGroup.GhostImpassable) != 0)
                continue;

            var tempXform = Transform(entity);

            var foo = _transform.GetMapCoordinates(entity, xform: tempXform).Position - _transform.GetMapCoordinates(ent, xform: xform).Position;
            _throwing.TryThrow(entity, foo * 2, component.ThrowStrength, ent, 0);
        }
    }
}
