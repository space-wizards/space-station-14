using System.Linq;
using System.Numerics;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class EntityAnomalySystem : SharedEntityAnomalySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
    }

    private void OnPulse(Entity<EntitySpawnAnomalyComponent> component, ref AnomalyPulseEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.SpawnOnPulse)
                continue;

            SpawnEntitesOnOpenTiles(component, entry, args.Stability, args.Severity);
        }
    }

    private void OnSupercritical(Entity<EntitySpawnAnomalyComponent> component, ref AnomalySupercriticalEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.SpawnOnSuperCritical)
                continue;

            SpawnEntitesOnOpenTiles(component, entry, 1, 1);
        }
    }

    private void OnStabilityChanged(Entity<EntitySpawnAnomalyComponent> component, ref AnomalyStabilityChangedEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.SpawnOnStabilityChanged)
                continue;

            SpawnEntitesOnOpenTiles(component, entry, args.Stability, args.Severity);
        }
    }

    private void OnSeverityChanged(Entity<EntitySpawnAnomalyComponent> component, ref AnomalySeverityChangedEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.SpawnOnSeverityChanged)
                continue;

            SpawnEntitesOnOpenTiles(component, entry, args.Stability, args.Severity);
        }
    }

    //TheShuEd:
    //I know it's a shitcode! I didn't write it! I just restructured the functions
    // To Do: make it reusable with TileAnomalySystem
    private void SpawnEntitesOnOpenTiles(Entity<EntitySpawnAnomalyComponent> component, EntitySpawnSettingsEntry entry, float stability, float severity)
    {
        if (entry.Spawns.Count == 0)
            return;

        var xform = Transform(component.Owner);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var amount = (int) (MathHelper.Lerp(entry.MinAmount, entry.MaxAmount, severity * stability) + 0.5f);

        var localpos = xform.Coordinates.Position;
        var tilerefs = grid.GetLocalTilesIntersecting(
            new Box2(localpos + new Vector2(-entry.MaxRange, -entry.MaxRange), localpos + new Vector2(entry.MaxRange, entry.MaxRange))).ToArray();

        if (tilerefs.Length == 0)
            return;

        _random.Shuffle(tilerefs);
        var physQuery = GetEntityQuery<PhysicsComponent>();
        var amountCounter = 0;
        foreach (var tileref in tilerefs)
        {
            //cut outer circle
            if (MathF.Sqrt(MathF.Pow(tileref.X - xform.LocalPosition.X, 2) + MathF.Pow(tileref.Y - xform.LocalPosition.Y, 2)) > entry.MaxRange)
                continue;

            //cut inner circle
            if (MathF.Sqrt(MathF.Pow(tileref.X - xform.LocalPosition.X, 2) + MathF.Pow(tileref.Y - xform.LocalPosition.Y, 2)) < entry.MinRange)
                continue;

            var valid = true;
            foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
            {
                if (!physQuery.TryGetComponent(ent, out var body))
                    continue;

                if (body.BodyType != BodyType.Static ||
                    !body.Hard ||
                    (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                    continue;

                valid = false;
                break;
            }
            if (!valid)
                continue;
            amountCounter++;
            Spawn(_random.Pick(entry.Spawns), tileref.GridIndices.ToEntityCoordinates(xform.GridUid.Value, _map));
            if (amountCounter >= amount)
                return;
        }
    }
}
