using System.Numerics;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class ThrowArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ThrowArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, ThrowArtifactComponent component, ArtifactActivatedEvent args)
    {
        var xform = Transform(uid);
        if (TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            var tiles = grid.GetTilesIntersecting(
                Box2.CenteredAround(_transform.GetWorldPosition(xform), new Vector2(component.Range * 2, component.Range)));

            foreach (var tile in tiles)
            {
                if (!_random.Prob(component.TilePryChance))
                    continue;

                _tile.PryTile(tile);
            }
        }

        var lookup = _lookup.GetEntitiesInRange(uid, component.Range, LookupFlags.Dynamic | LookupFlags.Sundries);
        var physQuery = GetEntityQuery<PhysicsComponent>();
        foreach (var ent in lookup)
        {
            if (physQuery.TryGetComponent(ent, out var phys)
                && (phys.CollisionMask & (int) CollisionGroup.GhostImpassable) != 0)
                continue;

            var tempXform = Transform(ent);

            var foo = _transform.GetMapCoordinates(ent, xform: tempXform).Position - _transform.GetMapCoordinates(uid, xform: xform).Position;
            _throwing.TryThrow(ent, foo*2, component.ThrowStrength, uid, 0);
        }
    }
}
