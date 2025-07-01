using Content.Server.Shuttles.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Slippery;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.Shuttles.Systems;

// shuttle impact damage ported from Goobstation (AGPLv3) with agreement of all coders involved
public sealed partial class ShuttleSystem
{
    private bool _enabled;
    private float _minimumImpactInertia;
    private float _minimumImpactVelocity;
    private float _tileBreakEnergyMultiplier;
    private float _damageMultiplier;
    private float _structuralDamage;
    private float _sparkEnergy;
    private float _impactRadius;
    private float _impactSlowdown;
    private float _minThrowVelocity;
    private float _massBias;
    private float _inertiaScaling;
    // this doesn't update if plating mass is changed but edgecase
    private float _platingMass;

    private const float _sparkChance = 0.2f;
    // shuttle mass to consider the neutral point for inertia scaling
    private const float _baseShuttleMass = 50f;
    // exists primarily for optimisation so not a cvar
    private const float _minImpulseVelocity = 0.07f;
    // high-speed collisions tend to be a series of increasingly smaller collisions so don't spam admin logs
    private readonly TimeSpan _adminLogSpacing = TimeSpan.FromSeconds(3);

    private readonly SoundCollectionSpecifier _shuttleImpactSound = new("ShuttleImpactSound");
    private readonly ProtoId<ContentTileDefinition> _platingId = "Plating";
    private readonly EntProtoId _sparkEffect = "EffectSparks";

    private EntityQuery<DamageableComponent> _dmgQuery;
    private EntityQuery<ProjectileComponent> _projQuery;

    private HashSet<EntityUid> _countedEnts = new();
    private HashSet<EntityUid> _intersecting = new();
    // for _adminLogSpacing
    private Dictionary<EntityUid, TimeSpan> _impactedAt = new();

    private void InitializeImpact()
    {
        SubscribeLocalEvent<ShuttleComponent, StartCollideEvent>(OnShuttleCollide);

        _dmgQuery = GetEntityQuery<DamageableComponent>();
        _projQuery = GetEntityQuery<ProjectileComponent>();

        Subs.CVar(_cfg, CCVars.ImpactEnabled, value => _enabled = value, true);
        Subs.CVar(_cfg, CCVars.MinimumImpactInertia, value => _minimumImpactInertia = value, true);
        Subs.CVar(_cfg, CCVars.MinimumImpactInertia, value => _minimumImpactInertia = value, true);
        Subs.CVar(_cfg, CCVars.MinimumImpactVelocity, value => _minimumImpactVelocity = value, true);
        Subs.CVar(_cfg, CCVars.TileBreakEnergyMultiplier, value => _tileBreakEnergyMultiplier = value, true);
        Subs.CVar(_cfg, CCVars.ImpactDamageMultiplier, value => _damageMultiplier = value, true);
        Subs.CVar(_cfg, CCVars.ImpactStructuralDamage, value => _structuralDamage = value, true);
        Subs.CVar(_cfg, CCVars.SparkEnergy, value => _sparkEnergy = value, true);
        Subs.CVar(_cfg, CCVars.ImpactRadius, value => _impactRadius = value, true);
        Subs.CVar(_cfg, CCVars.ImpactSlowdown, value => _impactSlowdown = value, true);
        Subs.CVar(_cfg, CCVars.ImpactMinThrowVelocity, value => _minThrowVelocity = value, true);
        Subs.CVar(_cfg, CCVars.ImpactMassBias, value => _massBias = value, true);
        Subs.CVar(_cfg, CCVars.ImpactInertiaScaling, value => _inertiaScaling = value, true);

        _platingMass = _protoManager.Index(_platingId).Mass;
    }

    /// <summary>
    /// Handles collision between two shuttles, applying impact damage and effects.
    /// </summary>
    private void OnShuttleCollide(EntityUid uid, ShuttleComponent component, ref StartCollideEvent args)
    {
        if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid)
            || TerminatingOrDeleted(args.OtherEntity) || EntityManager.IsQueuedForDeletion(args.OtherEntity)
        )
            return;

        if (!_gridQuery.TryComp(args.OurEntity, out var ourGrid) ||
            !_gridQuery.TryComp(args.OtherEntity, out var otherGrid)
        )
            return;

        var ourBody = args.OurBody;
        var otherBody = args.OtherBody;

        // TODO: Would also be nice to have a continuous sound for scraping.
        var ourXform = Transform(args.OurEntity);
        var otherXform = Transform(args.OtherEntity);
        var worldPoints = args.WorldPoints;
        var worldNormal = args.WorldNormal;

        for (var i = 0; i < worldPoints.Length; i++)
        {
            var worldPoint = worldPoints[i];

            var ourPoint = _transform.ToCoordinates((args.OurEntity, ourXform), new MapCoordinates(worldPoint, ourXform.MapID));
            var otherPoint = _transform.ToCoordinates((args.OtherEntity, otherXform), new MapCoordinates(worldPoint, otherXform.MapID));

            var ourVelocity = _physics.GetLinearVelocity(args.OurEntity, ourPoint.Position, ourBody, ourXform);
            var otherVelocity = _physics.GetLinearVelocity(args.OtherEntity, otherPoint.Position, otherBody, otherXform);
            var topDiff = (ourVelocity - otherVelocity);
            var jungleDiff = topDiff.Length();

            // Get the velocity in relation to the contact normal
            // If this still causes issues see https://box2d.org/posts/2020/06/ghost-collisions/
            // This should only be a potential problem on chunk seams.
            var dotProduct = MathF.Abs(Vector2.Dot(topDiff.Normalized(), worldNormal.Normalized()));
            jungleDiff *= dotProduct;

            // this is cursed but makes it so that collisions of small grid with large grid count the inertia as being approximately the small grid's
            var effectiveInertiaMult = (ourBody.FixturesMass * otherBody.FixturesMass) / (ourBody.FixturesMass + otherBody.FixturesMass);
            var effectiveInertia = jungleDiff * effectiveInertiaMult;

            // TODO: squish damage so that a tiny splinter grid can't stop 2 big grids by being in the way
            if (jungleDiff < _minimumImpactVelocity && effectiveInertia < _minimumImpactInertia
                || ourXform.MapUid == null
                || float.IsNaN(jungleDiff))
            {
                continue;
            }

            // Play impact sound
            var coordinates = new EntityCoordinates(ourXform.MapUid.Value, worldPoint);

            var volume = MathF.Min(10f, MathF.Pow(jungleDiff, 0.5f) - 5f);
            var audioParams = AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(volume);
            _audio.PlayPvs(_shuttleImpactSound, coordinates, audioParams);

            // if we're not enabled, stop after playing sound
            if (!_enabled)
                continue;

            // Convert the collision point directly to tile indices
            var ourTile = new Vector2i((int)Math.Floor(ourPoint.X / ourGrid.TileSize), (int)Math.Floor(ourPoint.Y / ourGrid.TileSize));
            var otherTile = new Vector2i((int)Math.Floor(otherPoint.X / otherGrid.TileSize), (int)Math.Floor(otherPoint.Y / otherGrid.TileSize));

            var ourMass = GetRegionMass(args.OurEntity, ourGrid, ourTile, _impactRadius, out var ourTiles);
            var otherMass = GetRegionMass(args.OtherEntity, otherGrid, otherTile, _impactRadius, out var otherTiles);

            // just in case
            if (ourTiles == 0 || otherTiles == 0)
                continue;

            Log.Info($"Shuttle impact of {ToPrettyString(args.OurEntity)} with {ToPrettyString(args.OtherEntity)}; our mass: {ourMass}, other: {otherMass}, velocity {jungleDiff}, impact point {worldPoint}");

            // E = MV^2/2
            var energyMult = MathF.Pow(jungleDiff, 2) / 2;
            // mass-based damage reduction to grid with more mass so that plastitanium block rammer doesn't die to lattice
            var ourMassDR = MathF.Max(otherMass / ourMass, 1f);
            var otherMassDR = MathF.Max(ourMass / otherMass, 1f);
            // multiplier to make large grids not just bonk against each other
            var inertiaMult = MathF.Pow(effectiveInertiaMult / _baseShuttleMass, _inertiaScaling);
            var toUsEnergy = otherMass * energyMult * inertiaMult * ourMassDR;
            var toOtherEnergy = ourMass * energyMult * inertiaMult * otherMassDR;

            var impact = LogImpact.High;
            // if impact isn't tiny, log it as extreme
            if (toUsEnergy + toOtherEnergy > 2f * _tileBreakEnergyMultiplier * _platingMass)
                impact = LogImpact.Extreme;
            // TODO: would be nice for it to also log who is piloting the grid(s)
            if (CheckShouldLog(args.OurEntity) && CheckShouldLog(args.OtherEntity))
                _logger.Add(LogType.ShuttleImpact, impact, $"Shuttle impact of {ToPrettyString(args.OurEntity)} with {ToPrettyString(args.OtherEntity)} at {worldPoint}");

            _impactedAt[args.OurEntity] = _gameTiming.CurTime;
            _impactedAt[args.OtherEntity] = _gameTiming.CurTime;

            // uses local region mass for slowdown calculation so lattice doesn't have same slowdown as wall block
            var totalInertia = ourVelocity * ourMass + otherVelocity * otherMass;
            var inelasticVel = totalInertia / (ourMass + otherMass);

            DoGridImpact((args.OurEntity, ourGrid, ourXform, ourBody), args.OurFixture, inelasticVel, ourVelocity, ourTile, ourTiles, toUsEnergy);
            DoGridImpact((args.OtherEntity, otherGrid, otherXform, otherBody), args.OtherFixture, inelasticVel, otherVelocity, otherTile, otherTiles, toOtherEnergy);
        }
    }

    private void DoGridImpact(Entity<MapGridComponent, TransformComponent, PhysicsComponent> ent,
                              Fixture fix,
                              Vector2 inelasticVelocity,
                              Vector2 velocity,
                              Vector2i tile,
                              int tiles,
                              float energy)
    {
        // for readability to not have .Comp1 .Comp2 for everything
        var (_, grid, xform, body) = ent;

        // radius in which to actually do things so we don't hurt person 4 tiles away on slow bump
        var radius = Math.Min(_impactRadius, MathF.Sqrt(energy / _tileBreakEnergyMultiplier / _platingMass));

        // slow us down since destroying impacting grid tiles prevents the collision
        // without this impacts which destroy tiles just make grids slice straight through each other
        var postImpactVelocity = Vector2.Lerp(velocity, inelasticVelocity, MathF.Min(1f, _impactSlowdown * tiles * fix.Density / body.FixturesMass));
        var deltaV = -velocity + postImpactVelocity;
        _physics.ApplyLinearImpulse(ent, deltaV * body.FixturesMass, body: body);

        // process tile and entity damage
        ProcessImpactZone(ent, grid, tile, energy, deltaV.Normalized(), radius);

        // throw every entity on grid if the impulse is not negligible
        if (deltaV.Length() > _minImpulseVelocity)
            ThrowEntitiesOnGrid(ent, xform, -deltaV);
    }

    /// <summary>
    /// Knocks and throws all unbuckled entities on the specified grid.
    /// </summary>
    private void ThrowEntitiesOnGrid(EntityUid gridUid, TransformComponent xform, Vector2 direction)
    {
        var movedByPressureQuery = GetEntityQuery<MovedByPressureComponent>();
        var knockdownTime = TimeSpan.FromSeconds(5);

        var minsq = _minThrowVelocity * _minThrowVelocity;
        // iterate all entities on the grid
        // TODO: only iterate non-static entities
        var childEnumerator = xform.ChildEnumerator;
        while (childEnumerator.MoveNext(out var uid))
        {
            // don't throw static bodies
            if (!_physicsQuery.TryGetComponent(uid, out var physics) || (physics.BodyType & BodyType.Static) != 0)
                continue;

            // don't throw if buckled
            if (_buckle.IsBuckled(uid, _buckleQuery.CompOrNull(uid)))
                continue;

            // don't throw them if they have magboots
            if (movedByPressureQuery.TryComp(uid, out var moved) && !moved.Enabled)
                continue;

            if (direction.LengthSquared() > minsq)
            {
                _stuns.TryKnockdown(uid, knockdownTime, true);
                _throwing.TryThrow(uid, direction, physics, Transform(uid), _projQuery, direction.Length(), playSound: false);
            }
            else
            {
                _physics.ApplyLinearImpulse(uid, direction * physics.Mass, body: physics);
            }
        }
    }

    /// <summary>
    /// Structure to hold impact tile processing data for batch processing
    /// </summary>
    private record struct ImpactTileData(Vector2i Tile, float Energy, float DistanceFactor);

    /// <summary>
    /// Gets the total mass of all entities and tiles (using ContentTileDefinition.Mass) belonging to this grid in a circle
    /// </summary>
    private float GetRegionMass(EntityUid uid, MapGridComponent grid, Vector2i centerTile, float radius, out int tileCount)
    {
        tileCount = 0;
        var mass = 0f;
        _countedEnts.Clear();

        foreach (var tileRef in _mapSystem.GetLocalTilesIntersecting(uid, grid, new Circle(centerTile, radius)))
        {
            var def = (ContentTileDefinition)_tileDefManager[tileRef.Tile.TypeId];
            mass += def.Mass;
            tileCount++;

            _intersecting.Clear();
            _lookup.GetLocalEntitiesIntersecting(uid, tileRef.GridIndices, _intersecting, gridComp: grid);
            foreach (var localUid in _intersecting)
            {
                if (!_countedEnts.Add(localUid))
                    continue;

                if (_physicsQuery.TryComp(localUid, out var physics))
                    mass += physics.FixturesMass;
            }
        }
        return mass;
    }

    /// <summary>
    /// Processes a zone of tiles around the impact point
    /// </summary>
    private void ProcessImpactZone(EntityUid uid, MapGridComponent grid, Vector2i centerTile, float energy, Vector2 dir, float radius)
    {
        // Create a list of all tiles to process
        var tilesToProcess = new List<ImpactTileData>();

        // Pre-calculate all tiles that need processing
        foreach (var tileRef in _mapSystem.GetLocalTilesIntersecting(uid, grid, new Circle(centerTile, radius)))
        {
            var distance = centerTile - tileRef.GridIndices;
            // Calculate distance-based energy falloff
            float distanceFactor = 1.0f - distance.Length / (radius + 1);
            float tileEnergy = energy * distanceFactor;

            tilesToProcess.Add(new ImpactTileData(tileRef.GridIndices, tileEnergy, distanceFactor));
        }

        // Process tiles sequentially for safety
        var brokenTiles = new List<(Vector2i, Tile)>();
        var sparkTiles = new List<Vector2i>();

        ProcessTileBatch(uid, grid, tilesToProcess, dir, 0, tilesToProcess.Count, brokenTiles, sparkTiles);

        // Only proceed with visual effects if the entity still exists
        if (Exists(uid))
        {
            ProcessBrokenTilesAndSparks(uid, grid, brokenTiles, sparkTiles);
        }
    }

    /// <summary>
    /// Process a batch of tiles from the impact zone
    /// </summary>
    private void ProcessTileBatch(
        EntityUid uid,
        MapGridComponent grid,
        List<ImpactTileData> tilesToProcess,
        Vector2 throwDirection,
        int startIndex,
        int endIndex,
        List<(Vector2i, Tile)> brokenTiles,
        List<Vector2i> sparkTiles)
    {
        // here so we don't have to `new` it every iteration
        var damageSpec = new DamageSpecifier()
        {
            DamageDict = { ["Blunt"] = 0, ["Structural"] = 0 }
        };

        var entitiesOnTile = new HashSet<Entity<TransformComponent>>();
        var tileCenter = new Vector2(grid.TileSize / 2f, grid.TileSize / 2f);

        for (var i = startIndex; i < endIndex; i++)
        {
            var tileData = tilesToProcess[i];

            bool canBreakTile = true;

            // Process entities on this tile
            entitiesOnTile.Clear();
            _lookup.GetLocalEntitiesIntersecting(uid, tileData.Tile, entitiesOnTile, gridComp: grid);

            // this loop is a hotspot so tell if you know how to optimise it
            foreach (var localEnt in entitiesOnTile)
            {
                // the query can ocassionally return entities barely touching this tile so check for that
                var toCenter = tileData.Tile + tileCenter - localEnt.Comp.Coordinates.Position;
                if (MathF.Abs(toCenter.X) > 0.5f || MathF.Abs(toCenter.Y) > 0.5f)
                    continue;

                if (_dmgQuery.TryComp(localEnt, out var damageable))
                {
                    // Apply damage scaled by distance but capped to prevent gibbing
                    var scaledDamage = tileData.Energy * _damageMultiplier;
                    damageSpec.DamageDict["Blunt"] = scaledDamage;
                    damageSpec.DamageDict["Structural"] = scaledDamage * _structuralDamage;

                    _damageSys.TryChangeDamage(localEnt, damageSpec, damageable: damageable);
                }
                // might've been destroyed
                if (TerminatingOrDeleted(localEnt) || EntityManager.IsQueuedForDeletion(localEnt))
                    continue;

                if (!_physicsQuery.TryComp(localEnt, out var physics))
                    continue;

                // no breaking tiles under walls that haven't been destroyed
                if ((physics.BodyType & BodyType.Static) != 0
                    && (physics.CollisionLayer & (int)CollisionGroup.Impassable) != 0)
                {
                    canBreakTile = false;
                }
                else
                {
                    var direction = throwDirection * tileData.DistanceFactor;
                    _throwing.TryThrow(localEnt, direction, physics, localEnt.Comp, _projQuery, direction.Length(), playSound: false);
                }
            }

            // Mark tiles for spark effects
            if (tileData.Energy > _sparkEnergy && tileData.DistanceFactor > 0.7f && _random.Prob(_sparkChance))
                sparkTiles.Add(tileData.Tile);

            if (!canBreakTile)
                continue;

            // Mark tiles for breaking/effects
            var def = (ContentTileDefinition)_tileDefManager[_mapSystem.GetTileRef(uid, grid, tileData.Tile).Tile.TypeId];
            if (tileData.Energy > def.Mass * _tileBreakEnergyMultiplier)
                brokenTiles.Add((tileData.Tile, Tile.Empty));

        }
    }

    /// <summary>
    /// Process visual effects and tile breaking after entity processing
    /// </summary>
    private void ProcessBrokenTilesAndSparks(
        EntityUid uid,
        MapGridComponent grid,
        List<(Vector2i, Tile)> brokenTiles,
        List<Vector2i> sparkTiles)
    {
        // Break tiles
        _mapSystem.SetTiles(uid, grid, brokenTiles);

        if (TerminatingOrDeleted(uid))
            return;

        // Spawn spark effects
        foreach (var tile in sparkTiles)
        {
            var coords = _mapSystem.GridTileToLocal(uid, grid, tile);
            Spawn(_sparkEffect, coords);
        }
    }

    /// <summary>
    /// Check whether this impact should be logged to admins.
    /// Used to prevent spamming logs.
    /// </summary>
    private bool CheckShouldLog(EntityUid uid)
    {
        return !(_impactedAt.ContainsKey(uid) && _gameTiming.CurTime < _impactedAt[uid] + _adminLogSpacing);
    }
}
