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

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact activation effect that pries tiles and throws stuff around.
/// </summary>
public sealed partial class XAEThrowThingsAroundSystem : BaseXAESystem<XAEThrowThingsAroundComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;

    private EntityQuery<PhysicsComponent> _physQuery;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<EntityUid> _entities = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _physQuery = GetEntityQuery<PhysicsComponent>();
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEThrowThingsAroundComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var component = ent.Comp;
        var xform = Transform(ent);
        if (TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            var areaForTilesPry = new Circle(_transform.GetWorldPosition(xform), component.Range);
            var tiles = _map.GetTilesIntersecting(xform.GridUid.Value, grid, areaForTilesPry, true);

            foreach (var tile in tiles)
            {
                if (!_random.Prob(component.TilePryChance))
                    continue;

                _tile.PryTile(tile);
            }
        }

        _entities.Clear();
        _lookup.GetEntitiesInRange(ent, component.Range, _entities, LookupFlags.Dynamic | LookupFlags.Sundries);
        foreach (var entity in _entities)
        {
            if (_physQuery.TryGetComponent(entity, out var phys)
                && (phys.CollisionMask & (int)CollisionGroup.GhostImpassable) != 0)
                continue;

            var tempXform = Transform(entity);

            var foo = _transform.GetWorldPosition(tempXform) - _transform.GetWorldPosition(xform);
            _throwing.TryThrow(entity, foo * 2, component.ThrowStrength, ent, 0);
        }
    }
}
