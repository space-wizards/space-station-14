using System.Numerics;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.CombatMode;
using Content.Shared.Containers;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Flamethrower;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Shared.Popups;

namespace Content.Server.DeadSpace.Flamethrower;

public sealed class ContinuousFlamethrowerSystem : EntitySystem
{
    private const float TickRate = 0.1f;
    private const float SampleSpacing = 0.32f;
    private const float FlameCollisionHalfWidth = 0.24f;
    private static readonly ReagentId Phlogiston = new("Phlogiston", null);
    private static readonly ReagentId Napalm = new("Napalm", null);

    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SlotBasedConnectedContainerSystem _connected = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private readonly Dictionary<EntityUid, ActiveFlame> _active = new();

    private readonly HashSet<EntityUid> _noFuelUsers = new();
    private float _timer;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<FlamethrowerInputEvent>(OnInput);
        SubscribeLocalEvent<FlamethrowerBurningComponent, ExtinguishEvent>(OnExtinguished);
        SubscribeLocalEvent<FlamethrowerFuelTankComponent, SolutionContainerChangedEvent>(OnTankSolutionChanged);
    }

    private bool HasFuel(EntityUid weapon)
    {
        return _connected.TryGetConnectedContainer(weapon, out var tank) &&
               tank is { } tankUid &&
               _solutions.TryGetSolution(tankUid, "tank", out _, out var solution) &&
               HasOnlyFuel(solution);
    }
    public override void Shutdown()
    {
        foreach (var flame in _active.Values)
        {
            _audio.Stop(flame.AudioStream);
            if (TryComp<Robust.Server.GameObjects.PointLightComponent>(flame.Weapon, out var light))
                _pointLight.SetEnabled(flame.Weapon, false, light);
        }
        _active.Clear();
        base.Shutdown();
    }

    private void OnExtinguished(Entity<FlamethrowerBurningComponent> ent, ref ExtinguishEvent args)
    {
        RemCompDeferred<FlamethrowerBurningComponent>(ent);
    }

    private void OnTankSolutionChanged(Entity<FlamethrowerFuelTankComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != "tank" ||
            !_solutions.TryGetSolution(ent.Owner, "tank", out var solutionEnt, out var solution))
            return;

        foreach (var reagent in solution.Contents.ToArray())
        {
            if (reagent.Reagent.Prototype == Phlogiston.Prototype ||
                reagent.Reagent.Prototype == Napalm.Prototype)
                continue;

            _solutions.RemoveReagent(solutionEnt.Value, reagent);
        }
    }

    private void OnInput(FlamethrowerInputEvent args, EntitySessionEventArgs session)
    {
        if (session.SenderSession.AttachedEntity is not { } user)
            return;

        var weapon = GetEntity(args.Weapon);
        if (!args.Active)
        {
            if (_active.TryGetValue(user, out var released))
                released.Released = true;
            _noFuelUsers.Remove(user);
            return;
        }

        if (!_combatMode.IsInCombatMode(user) ||
            !_actionBlocker.CanAttack(user) ||
            !HasComp<ContinuousFlamethrowerComponent>(weapon) ||
            !_hands.IsHolding(user, weapon))
            return;

        var target = GetCoordinates(args.Target);
        if (!target.IsValid(EntityManager))
            return;

        var comp = Comp<ContinuousFlamethrowerComponent>(weapon);

        if (_active.TryGetValue(user, out var flame) && flame.Weapon == weapon)
        {
            flame.Target = target;
            flame.Released = false;
            return;
        }

        if (_noFuelUsers.Contains(user))
            return;

        if (!HasFuel(weapon))
        {
            _popup.PopupEntity(Loc.GetString("flamethrower-out-of-fuel"), user, user);
            _noFuelUsers.Add(user);
            return;
        }

        if (flame != null)
            StopFlame(user);

        _active[user] = CreateFlame(user, weapon, target, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _timer += frameTime;
        if (_timer < TickRate)
            return;
        _timer -= TickRate;

        foreach (var (user, flame) in _active.ToArray())
        {
            if (!_combatMode.IsInCombatMode(user) ||
                !_actionBlocker.CanAttack(user) ||
                !TryComp<ContinuousFlamethrowerComponent>(flame.Weapon, out var comp) ||
                !_hands.IsHolding(user, flame.Weapon))
            {
                StopFlame(user);
                continue;
            }
        
            if (!_connected.TryGetConnectedContainer(flame.Weapon, out var tank) ||
                tank is not { } tankUid ||
                !_solutions.TryGetSolution(tankUid, "tank", out var solutionEnt, out var solution) ||
                !HasOnlyFuel(solution))
            {
                StopFlame(user);
                if (_noFuelUsers.Add(user))
                    _popup.PopupEntity(Loc.GetString("flamethrower-out-of-fuel"), user, user);
                continue;
            }
        
            var origin = _transform.GetMapCoordinates(user);
            var requested = _transform.ToMapCoordinates(flame.Target);
            if (origin.MapId != requested.MapId)
            {
                StopFlame(user);
                continue;
            }
        
            var delta = requested.Position - origin.Position;
            var distance = Math.Clamp(delta.Length(), comp.MinimumRange, comp.MaxRange);
            var direction = delta.LengthSquared() > 0.001f ? Vector2.Normalize(delta) : Vector2.UnitX;
            var fuelMultiplier = 1f + (comp.MaximumRangeFuelMultiplier - 1f) * distance / comp.MaxRange;
            var fuelCost = FixedPoint2.New(comp.FuelPerTick * fuelMultiplier);
            if (solution.Volume < fuelCost)
            {
                StopFlame(user);
                if (_noFuelUsers.Add(user))
                    _popup.PopupEntity(Loc.GetString("flamethrower-out-of-fuel"), user, user);
                continue;
            }

            _solutions.SplitSolution(solutionEnt.Value, fuelCost);
            var points = BuildFlame(origin, direction, distance, user);
            ApplyFlame(user, flame.Weapon, points);

            var netPoints = new List<NetCoordinates>(points.Count);
            var mapUid = Transform(user).MapUid;
            if (mapUid == null)
            {
                StopFlame(user);
                continue;
            }
            foreach (var point in points)
                netPoints.Add(GetNetCoordinates(new EntityCoordinates(mapUid.Value, point)));
            RaiseNetworkEvent(new FlamethrowerVisualEvent(netPoints), Filter.Pvs(user));

            // Even a very short click gets one complete server fire tick.
            if (flame.Released)
                StopFlame(user);
        }
    }

    private bool HasOnlyFuel(Solution solution)
    {
        if (solution.Volume <= 0 ||
            solution.GetReagentQuantity(Phlogiston) <= 0 ||
            solution.GetReagentQuantity(Napalm) <= 0)
            return false;

        foreach (var reagent in solution.Contents)
        {
            if (reagent.Reagent.Prototype != Phlogiston.Prototype &&
                reagent.Reagent.Prototype != Napalm.Prototype)
                return false;
        }
        return true;
    }

    private List<Vector2> BuildFlame(MapCoordinates origin, Vector2 direction, float distance, EntityUid user)
    {
        var points = new List<Vector2>();
        var side = new Vector2(-direction.Y, direction.X);
        var mainDistance = distance;

        // A flame jet has width. Tracing both edges prevents the centre ray from slipping
        // through the tiny diagonal gap at a wall corner.
        foreach (var offset in new[] { 0f, FlameCollisionHalfWidth, -FlameCollisionHalfWidth })
        {
            var ray = new CollisionRay(
                origin.Position + side * offset,
                direction,
                (int) CollisionGroup.FullTileMask);
            var hit = _physics.IntersectRay(origin.MapId, ray, distance, user, true).FirstOrNull();
            if (hit != null)
                mainDistance = Math.Min(mainDistance, hit.Value.Distance);
        }

        AddLine(points, origin.Position + direction * 0.55f, direction, Math.Max(0f, mainDistance - 0.55f));
        return points;
    }
    private static void AddLine(List<Vector2> points, Vector2 start, Vector2 direction, float length)
    {
        var count = Math.Max(1, (int) (length / SampleSpacing));
        for (var i = 0; i <= count; i++)
            points.Add(start + direction * Math.Min(length, i * SampleSpacing));
    }

    private void ApplyFlame(EntityUid user, EntityUid weapon, List<Vector2> points)
    {
        var hit = new HashSet<EntityUid>();
        var damage = new DamageSpecifier { DamageDict = { ["Heat"] = FixedPoint2.New(3f) } };

        foreach (var point in points)
        {
            var coords = new MapCoordinates(point, Transform(user).MapID);
            foreach (var target in _lookup.GetEntitiesInRange(coords, 0.34f))
            {
                if (target == user ||
                    !hit.Add(target) ||
                    _containers.IsEntityInContainer(target))
                    continue;

                var targetCoordinates = _transform.GetMapCoordinates(target);
                var fromFlame = targetCoordinates.Position - point;
                if (targetCoordinates.MapId != coords.MapId)
                    continue;

                var targetDistance = fromFlame.Length();
                if (targetDistance > 0.001f)
                {
                    var targetRay = new CollisionRay(
                        point,
                        Vector2.Normalize(fromFlame),
                        (int) CollisionGroup.FullTileMask);
                    var obstruction = _physics.IntersectRay(
                        coords.MapId,
                        targetRay,
                        targetDistance,
                        user,
                        true).FirstOrNull();
                    if (obstruction is { } wall && wall.HitEntity != target)
                        continue;
                }

                if (HasComp<KudzuComponent>(target))
                {
                    QueueDel(target);
                    continue;
                }

                if (!HasComp<DamageableComponent>(target))
                    continue;

                _damage.TryChangeDamage(target, damage, origin: user, interruptsDoAfters: false);
                if (!TryComp<FlammableComponent>(target, out var flammable))
                    continue;

                EnsureComp<FlamethrowerBurningComponent>(target);
                _flammable.AdjustFireStacks(target, 0.2f, flammable, true);
                _flammable.Ignite(target, weapon, flammable, user);
            }
        }
    }

    private ActiveFlame CreateFlame(
        EntityUid user,
        EntityUid weapon,
        EntityCoordinates target,
        ContinuousFlamethrowerComponent comp)
    {
        _audio.PlayPvs(comp.ShotSound, user);
        var stream = _audio.PlayPvs(
            comp.AmbientSound,
            user,
            comp.AmbientSound.Params.WithLoop(true))?.Entity;
        if (TryComp<Robust.Server.GameObjects.PointLightComponent>(weapon, out var light))
            _pointLight.SetEnabled(weapon, true, light);
        return new ActiveFlame(weapon, target, stream);
    }

    private void StopFlame(EntityUid user)
    {
        if (!_active.Remove(user, out var flame))
            return;

        _audio.Stop(flame.AudioStream);
        if (TryComp<Robust.Server.GameObjects.PointLightComponent>(flame.Weapon, out var light))
            _pointLight.SetEnabled(flame.Weapon, false, light);
    }

    private sealed class ActiveFlame(
        EntityUid weapon,
        EntityCoordinates target,
        EntityUid? audioStream)
    {
        public EntityUid Weapon = weapon;
        public EntityCoordinates Target = target;
        public EntityUid? AudioStream = audioStream;
        public bool Released;
    }
}
