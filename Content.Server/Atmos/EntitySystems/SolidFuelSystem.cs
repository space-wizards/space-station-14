using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects.Effects.Atmos;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Robust.Shared.Containers;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics.Components;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>Contact ignition and finite fuel for combustible objects.</summary>
public sealed partial class SolidFuelSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private TileSystem _tileSystem = default!;

    public bool Enabled => _config.GetCVar(CCVars.SolidFuelEnabled);

    private readonly HashSet<Entity<SolidFuelComponent>> _nearby = new();
    private readonly HashSet<Entity<PuddleComponent>> _puddles = new();
    private readonly Dictionary<EntityUid, (EntityUid Source, float Rate, EntityUid? User)> _exposures = new();
    private float _elapsed;
    private readonly HashSet<Entity<SolidFuelComponent>> _floorCandidates = new();
    private readonly HashSet<EntityUid> _sources = new();
    private static readonly Vector2i[] Neighbors = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    /// <summary>Spray tile reactions must also reach non-colliding carpet and floor fire entities.</summary>
    public void ExtinguishTile(TileRef tile, float amount)
    {
        if (amount <= 0)
            return;
        var center = new EntityCoordinates(tile.GridUid,
            new System.Numerics.Vector2(tile.GridIndices.X + 0.5f, tile.GridIndices.Y + 0.5f));
        var targets = new HashSet<Entity<SolidFuelComponent>>();
        _lookup.GetEntitiesInRange(center, 0.71f, targets, LookupFlags.Uncontained);
        foreach (var target in targets)
        {
            // Collidable objects already receive the vapor's ordinary entity reaction.
            if (TryComp<PhysicsComponent>(target, out var body) && body.CanCollide)
                continue;
            if (_turf.GetTileRef(Transform(target).Coordinates) is not { } other ||
                other.GridUid != tile.GridUid || other.GridIndices != tile.GridIndices)
                continue;
            target.Comp.Exposure = 0;
            var ev = new ExtinguishEvent { FireStacksAdjustment = -1.5f * amount };
            RaiseLocalEvent(target, ref ev);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(FlammableSystem));
        SubscribeLocalEvent<SolidFuelComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SolidFuelComponent, SolidFuelIgnitionDoAfterEvent>(OnIgnitionDoAfter);
        SubscribeLocalEvent<SolidFuelComponent, DoAfterAttemptEvent<SolidFuelIgnitionDoAfterEvent>>(OnIgnitionAttempt);
        SubscribeLocalEvent<SolidFuelComponent, ExtinguishedEvent>(OnExtinguished);
        Subs.SubscribeWithRelay<SolidFuelComponent, ExtinguishEvent>(OnExtinguish);
        SubscribeLocalEvent<SolidFuelComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnExtinguished(Entity<SolidFuelComponent> ent, ref ExtinguishedEvent args)
    {
        ent.Comp.Exposure = 0;
        _exposures.Remove(ent);
    }

    private void OnShutdown(Entity<SolidFuelComponent> ent, ref ComponentShutdown args)
    {
        _exposures.Remove(ent);
    }

    private void OnExtinguish(Entity<SolidFuelComponent> ent, ref ExtinguishEvent args)
    {
        ent.Comp.Exposure = 0;
        _exposures.Remove(ent);
        if (args.FireStacksAdjustment < 0)
            ent.Comp.WetTime = Math.Clamp(ent.Comp.WetTime - args.FireStacksAdjustment, 0, 10);
    }

    /// <summary>Returns contact heating from a lit cigarette, burning entity, or active ignition source.</summary>
    public float GetIgnitionRate(EntityUid source)
    {
        if (TryComp<SmokableComponent>(source, out var smoke))
            return smoke.State == SmokableState.Lit ? MathF.Max(0, _config.GetCVar(CCVars.SolidFuelCigaretteRate)) : 0f;
        if (TryComp<FlammableComponent>(source, out var fire) && fire.OnFire)
            return MathF.Max(0, _config.GetCVar(CCVars.SolidFuelFireRate));
        return TryComp<IgnitionSourceComponent>(source, out var ignition) && ignition.Ignited
            ? ignition.ContactIgnitionRate : 0f;
    }

    /// <summary>Checks the enable switch, fire stacks, recent and current wetness, and available oxygen.</summary>
    public bool CanBurn(Entity<FlammableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return Enabled && ent.Comp.FireStacks >= 0 &&
               (!TryComp<SolidFuelComponent>(ent, out var fuel) || fuel.WetTime <= 0) && !IsWet(ent) &&
               HasOxygen(ent);
    }

    /// <summary>Checks local oxygen, falling back to adjacent tiles for airtight entities such as wooden walls.</summary>
    public bool HasOxygen(EntityUid uid)
    {
        if (_atmos.GetContainingMixture(uid) is { } air && air.GetMoles(Gas.Oxygen) >= 1f)
            return true;
        // Solid wooden walls occupy an airtight tile, but their exposed faces can burn.
        if (!HasComp<AirtightComponent>(uid) || _turf.GetTileRef(Transform(uid).Coordinates) is not { } tile)
            return false;
        foreach (var offset in Neighbors)
        {
            if (_atmos.GetTileMixture(tile.GridUid, Transform(uid).MapUid, tile.GridIndices + offset) is { } adjacent &&
                adjacent.GetMoles(Gas.Oxygen) >= 1f)
                return true;
        }
        return false;
    }

    private bool IsWet(EntityUid uid)
    {
        // Absorbed water does not produce negative fire stacks when a towel mops a puddle.
        if (TryComp<AbsorbentComponent>(uid, out var absorbent) &&
            HasWater(uid, absorbent.SolutionName))
            return true;

        if (_containers.IsEntityOrParentInContainer(uid))
            return false;

        _puddles.Clear();
        _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 0.5f, _puddles);
        foreach (var puddle in _puddles)
        {
            if (HasWater(puddle.Owner, puddle.Comp.SolutionName))
                return true;
        }
        return false;
    }

    private bool HasWater(EntityUid uid, string solutionName)
    {
        if (!_solutions.TryGetSolution(uid, solutionName, out _, out var solution))
            return false;

        // Respect reagent definitions, including water, drinks and extinguishing chemicals.
        foreach (var (reagent, quantity) in solution.Contents)
        {
            if (quantity <= 0 || !_prototypes.TryIndex<ReagentPrototype>(reagent.Prototype, out var proto) ||
                proto.ReactiveEffects == null || !proto.ReactiveEffects.TryGetValue("Extinguish", out var reaction))
                continue;
            foreach (var effect in reaction.Effects)
            {
                if (effect is Extinguish)
                    return true;
            }
        }
        return false;
    }

    private void OnInteractUsing(Entity<SolidFuelComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || GetIgnitionRate(args.Used) <= 0 ||
            !TryComp<FlammableComponent>(ent, out var fire) || fire.OnFire || !CanBurn((ent, fire)))
            return;

        args.Handled = true;
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, 1f,
            new SolidFuelIgnitionDoAfterEvent(), ent, target: ent, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        });
    }

    private void OnIgnitionAttempt(Entity<SolidFuelComponent> ent,
        ref DoAfterAttemptEvent<SolidFuelIgnitionDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Used is not { } source || GetIgnitionRate(source) <= 0 ||
            !TryComp<FlammableComponent>(ent, out var fire) || fire.OnFire || !CanBurn((ent, fire)))
            args.Cancel();
    }

    private void OnIgnitionDoAfter(Entity<SolidFuelComponent> ent, ref SolidFuelIgnitionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } source ||
            !TryComp<FlammableComponent>(ent, out var fire) || fire.OnFire || !CanBurn((ent, fire)))
            return;

        var rate = GetIgnitionRate(source);
        if (rate <= 0)
            return;

        Expose(ent, source, rate, args.User);
        args.Handled = true;
        args.Repeat = true;
    }

    private void Expose(EntityUid target, EntityUid source, float rate, EntityUid? user = null)
    {
        // Multiple sources use the strongest contact, rather than multiplying the update frequency.
        if (!_exposures.TryGetValue(target, out var old) || rate > old.Rate)
            _exposures[target] = (source, rate, user);
    }

    private void HeatNearby(EntityUid source)
    {
        var rate = GetIgnitionRate(source);
        if (rate <= 0 || _containers.IsEntityOrParentInContainer(source))
            return;

        var burning = TryComp<FlammableComponent>(source, out var fire) && fire.OnFire;
        if (burning && HasComp<SolidFuelComponent>(source) && !CanBurn((source, fire!)))
            return;
        if (burning && !_config.GetCVar(CCVars.SolidFuelSpread))
            return;
        var range = Math.Clamp(_config.GetCVar(burning ? CCVars.SolidFuelSpreadRange : CCVars.SolidFuelContactRange), 0f, 3f);
        if (range <= 0)
            return;

        HeatFloor(source, rate, range);

        _nearby.Clear();
        _lookup.GetEntitiesInRange(Transform(source).Coordinates, range, _nearby, LookupFlags.Uncontained);
        foreach (var target in _nearby)
        {
            if (source != target.Owner &&
                _interaction.InRangeUnobstructed(source, target.Owner, range: range))
                Expose(target, source, rate);
        }
    }

    private void HeatFloor(EntityUid source, float rate, float range)
    {
        var coords = Transform(source).Coordinates;
        var radius = (int) MathF.Ceiling(range);
        for (var x = -radius; x <= radius; x++)
        for (var y = -radius; y <= radius; y++)
        {
            if (x * x + y * y > range * range)
                continue;
            var tile = _turf.GetTileRef(coords.Offset(new System.Numerics.Vector2(x, y)));
            if (tile is not { } floor ||
                ((ContentTileDefinition) _tiles[floor.Tile.TypeId]).SolidFuelEntity is not { } prototype)
                continue;
            var center = new EntityCoordinates(floor.GridUid,
                new System.Numerics.Vector2(floor.GridIndices.X + 0.5f, floor.GridIndices.Y + 0.5f));
            // Query actual entities so map loading and grid splitting cannot leave a stale cache.
            EntityUid? existing = null;
            _floorCandidates.Clear();
            _lookup.GetEntitiesInRange(center, 0.1f, _floorCandidates, LookupFlags.Uncontained);
            foreach (var candidate in _floorCandidates)
            {
                if (candidate.Comp.TileType != null && !TerminatingOrDeleted(candidate) &&
                    !EntityManager.IsQueuedForDeletion(candidate))
                {
                    existing = candidate.Owner;
                    break;
                }
            }
            var fuel = existing ?? Spawn(prototype, center);
            if (existing == null)
                Comp<SolidFuelComponent>(fuel).TileType = ((ContentTileDefinition) _tiles[floor.Tile.TypeId]).ID;
            if (fuel != source && _interaction.InRangeUnobstructed(source, fuel, range: range + 0.71f))
                Expose(fuel, source, rate);
        }
    }

    public override void Update(float frameTime)
    {
        _elapsed += frameTime;
        if (_elapsed < 1f)
            return;
        var elapsed = _elapsed;
        _elapsed = 0;

        if (!Enabled)
        {
            var disabled = EntityQueryEnumerator<SolidFuelComponent, FlammableComponent>();
            while (disabled.MoveNext(out var uid, out var fuel, out var fire))
            {
                fuel.Exposure = 0;
                _flammable.TryExtinguish((uid, fire));
                if (fuel.TileType != null)
                    QueueDel(uid);
            }
            _exposures.Clear();
            return;
        }

        _sources.Clear();
        var sources = EntityQueryEnumerator<IgnitionSourceComponent>();
        while (sources.MoveNext(out var uid, out _))
            _sources.Add(uid);
        var cigarettes = EntityQueryEnumerator<SmokableComponent>();
        while (cigarettes.MoveNext(out var uid, out _))
            _sources.Add(uid);
        var fires = EntityQueryEnumerator<FlammableComponent>();
        while (fires.MoveNext(out var uid, out var fire))
        {
            if (fire.OnFire)
                _sources.Add(uid);
        }
        foreach (var source in _sources)
            HeatNearby(source);

        var fuels = EntityQueryEnumerator<SolidFuelComponent, FlammableComponent>();
        while (fuels.MoveNext(out var uid, out var fuel, out var fire))
        {
            fuel.WetTime = MathF.Max(0, fuel.WetTime - elapsed);
            if (fuel.TileType is { } tileType &&
                (_turf.GetTileRef(Transform(uid).Coordinates) is not { } tile ||
                 ((ContentTileDefinition) _tiles[tile.Tile.TypeId]).ID != tileType.Id))
            {
                QueueDel(uid);
                continue;
            }
            // Cold objects do not need atmosphere or puddle lookups.
            if (!fire.OnFire && fuel.Exposure <= 0 && !_exposures.ContainsKey(uid))
            {
                if (fuel.TileType != null && fuel.BurnedTime <= 0 && fuel.WetTime <= 0)
                    QueueDel(uid);
                continue;
            }
            if (!CanBurn((uid, fire)))
            {
                fuel.Exposure = 0;
                _flammable.TryExtinguish((uid, fire));
                if (fuel.TileType != null && fuel.BurnedTime <= 0 && fuel.WetTime <= 0)
                    QueueDel(uid);
                continue;
            }

            if (fire.OnFire)
            {
                fuel.Exposure = 0;
                fuel.BurnedTime += elapsed * MathF.Max(0, _config.GetCVar(CCVars.SolidFuelBurnMultiplier));
                if (fuel.BurnedTime >= fuel.BurnTime)
                {
                    if (fuel.TileType != null && _turf.GetTileRef(Transform(uid).Coordinates) is { } floor)
                        _tileSystem.DeconstructTile(floor, spawnItem: false);
                    SpawnNextToOrDrop(fuel.AshPrototype, uid);
                    QueueDel(uid);
                }
                continue;
            }

            if (_exposures.TryGetValue(uid, out var exposure) && !Deleted(exposure.Source) &&
                GetIgnitionRate(exposure.Source) > 0)
            {
                fuel.Exposure += exposure.Rate * elapsed * MathF.Max(0, _config.GetCVar(CCVars.SolidFuelIgnitionMultiplier));
                if (fuel.Exposure >= fuel.IgnitionTime)
                    _flammable.Ignite(uid, exposure.Source, fire, exposure.User);
            }
            else
                fuel.Exposure = MathF.Max(0, fuel.Exposure - fuel.CoolingRate * elapsed);

            if (fuel.TileType != null && fuel.Exposure <= 0 && fuel.BurnedTime <= 0 && fuel.WetTime <= 0 && !fire.OnFire)
                QueueDel(uid);
        }
        _exposures.Clear();
    }
}
