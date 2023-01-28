using System.Linq;
using Content.Server.Coordinates.Helpers;
using Content.Server.Decals;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class FleshAnomalySystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly DecalSystem _decal = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FleshAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<FleshAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<FleshAnomalyComponent, AnomalyStabilityChangedEvent>(OnSeverityChanged);
    }

    private void OnPulse(EntityUid uid, FleshAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var range = component.SpawnRange * args.Stability;
        var amount = (int) (component.MaxSpawnAmount * args.Severity + 0.5f);

        var xform = Transform(uid);
        SpawnMonstersOnOpenTiles(component, xform, amount, range);
    }

    private void OnSupercritical(EntityUid uid, FleshAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(uid);
        SpawnMonstersOnOpenTiles(component, xform, component.MaxSpawnAmount, component.SpawnRange);
        EntityManager.SpawnEntity(component.SupercriticalSpawn, xform.Coordinates);
    }

    private void OnSeverityChanged(EntityUid uid, FleshAnomalyComponent component, ref AnomalyStabilityChangedEvent args)
    {
        var xform = Transform(uid);
        if (!_map.TryGetGrid(xform.GridUid, out var grid))
            return;

        var range = component.SpawnRange * args.Stability;
        var fleshTile = _tile[component.FleshTileId].TileId;
        var worldPos = _xform.GetWorldPosition(xform);
        var tilerefs = grid.GetTilesIntersecting(new Circle(worldPos, range)).ToArray();
        foreach (var tileref in tilerefs)
        {
            if (!_random.Prob(0.33f))
                continue;
            var variant = _random.Pick(((ContentTileDefinition) _tile[fleshTile]).PlacementVariants);
            grid.SetTile(tileref.GridIndices, new Tile(fleshTile, 0, variant));

            var decals = _decal.GetDecalsInRange(tileref.GridUid, tileref.GridPosition().SnapToGrid(EntityManager, _map).Position, 0.5f);
            foreach (var (id, _) in decals)
            {
                _decal.RemoveDecal(tileref.GridUid, id);
            }
        }
    }

    private void SpawnMonstersOnOpenTiles(FleshAnomalyComponent component, TransformComponent xform, int toSpawn, float range)
    {
        if (!_map.TryGetGrid(xform.GridUid, out var grid))
            return;

        var worldPos = _xform.GetWorldPosition(xform);
        var tilerefs = grid.GetTilesIntersecting(new Circle(worldPos, range)).ToArray();
        var validSpawnLocations = new List<Vector2i>();
        var physQuery = GetEntityQuery<PhysicsComponent>();
        foreach (var tileref in tilerefs)
        {
            var valid = true;
            foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
            {
                if (!physQuery.TryGetComponent(ent, out var body))
                    continue;
                if (body.BodyType == BodyType.Static &&
                    body.Hard &&
                    (body.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                {
                    valid = false;
                    break;
                }
            }
            if (valid)
                validSpawnLocations.Add(tileref.GridIndices);
        }

        if (!validSpawnLocations.Any())
            return;

        for (var i = 0; i < toSpawn; i++)
        {
            var pos = _random.Pick(validSpawnLocations);
            EntityManager.SpawnEntity(_random.Pick(component.Spawns), pos.ToEntityCoordinates(xform.GridUid.Value, _map));
        }
    }
}
