using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Gravity;

public abstract partial class SharedGravitySystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public static readonly ProtoId<AlertPrototype> WeightlessAlert = "Weightless";

    private EntityQuery<GravityComponent> _gravityQuery;
    protected EntityQuery<TransformComponent> XformQuery;

    public override void Initialize()
    {
        base.Initialize();
        // Grid Gravity
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<GravityChangedEvent>(OnGravityChange);
        SubscribeLocalEvent<GravityComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<GravityComponent, ComponentHandleState>(OnHandleState);

        // Weightlessness
        SubscribeLocalEvent<WeightlessnessComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WeightlessnessComponent, EntParentChangedMessage>(OnEntParentChanged);
        SubscribeLocalEvent<WeightlessnessComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChanged);

        // Alerts
        SubscribeLocalEvent<AlertSyncEvent>(OnAlertsSync);
        SubscribeLocalEvent<AlertsComponent, WeightlessnessChangedEvent>(OnWeightlessnessChanged);
        SubscribeLocalEvent<AlertsComponent, EntParentChangedMessage>(OnAlertsParentChange);

        // Things that care about weightlessness
        SubscribeLocalEvent<WeightlessnessComponent, ShooterImpulseEvent>(OnShooterImpulse);
        SubscribeLocalEvent<WeightlessnessComponent, ThrowerImpulseEvent>(OnThrowerImpulse);
        SubscribeLocalEvent<WeightlessnessComponent, KnockDownAttemptEvent>(OnKnockdownAttempt);
        SubscribeLocalEvent<WeightlessnessComponent, GetStandUpTimeEvent>(OnGetStandUpTime);

        _gravityQuery = GetEntityQuery<GravityComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateShake();
    }

    [Obsolete("Use the Entity<WeightlessnessComponent?> overload instead.")]
    public bool IsWeightless(EntityUid uid, PhysicsComponent body, TransformComponent? xform = null)
    {
        if (TryComp<WeightlessnessComponent>(uid, out var weightless))
            return IsWeightless((uid, weightless));

        if ((body.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0)
            return false;

        var ev = new IsWeightlessEvent();
        RaiseLocalEvent(uid, ref ev);
        if (ev.Handled)
            return ev.IsWeightless;

        if (!Resolve(uid, ref xform))
            return true;

        // If grid / map has gravity
        if (EntityGridOrMapHaveGravity((uid, xform)))
            return false;

        return true;
    }

    public bool IsWeightless(Entity<WeightlessnessComponent?> entity)
    {
        // If we can be weightless and are weightless, return true, otherwise return false
        return Resolve(entity, ref entity.Comp, false) && entity.Comp.Weightless;
    }

    private bool TryWeightless(Entity<WeightlessnessComponent, PhysicsComponent?, TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2, false))
            return false;

        if (entity.Comp2.BodyType is BodyType.Static or BodyType.Kinematic)
            return false;

        if (!Resolve(entity, ref entity.Comp3))
            return true;

        // Check if something other than the grid or map is overriding our gravity
        var ev = new IsWeightlessEvent();
        RaiseLocalEvent(entity, ref ev);
        if (ev.Handled)
            return ev.IsWeightless;

        return !EntityGridOrMapHaveGravity((entity, entity.Comp3));
    }

    /// <summary>
    /// Refreshes weightlessness status, needs to be called anytime it would change.
    /// </summary>
    /// <param name="entity">The entity we are updating the weightless status of</param>
    /// <param name="weightless">The weightless value we are trying to change to, helps avoid needless networking</param>
    public void RefreshWeightless(Entity<WeightlessnessComponent?> entity, bool? weightless = null)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        // Only update if we're changing our weightless status
        if (entity.Comp.Weightless == weightless)
            return;

        ChangeWeightless(entity!);
    }

    private void ChangeWeightless(Entity<WeightlessnessComponent> entity)
    {
        var newWeightless = TryWeightless(entity);

        if (newWeightless == entity.Comp.Weightless)
            return;

        entity.Comp.Weightless = newWeightless;
        Dirty(entity);

        var ev = new WeightlessnessChangedEvent(entity.Comp.Weightless);
        RaiseLocalEvent(entity, ref ev);
    }

    private void OnMapInit(Entity<WeightlessnessComponent> entity, ref MapInitEvent args)
    {
        RefreshWeightless((entity.Owner, entity.Comp));
    }

    private void OnWeightlessnessChanged(Entity<AlertsComponent> entity, ref WeightlessnessChangedEvent args)
    {
        if (args.Weightless)
            _alerts.ShowAlert(entity, WeightlessAlert);
        else
            _alerts.ClearAlert(entity, WeightlessAlert);
    }

    private void OnEntParentChanged(Entity<WeightlessnessComponent> entity, ref EntParentChangedMessage args)
    {
        // If we've moved but are still on the same grid, then don't do anything.
        if (args.OldParent == args.Transform.GridUid)
            return;

        RefreshWeightless((entity.Owner, entity.Comp), !EntityGridOrMapHaveGravity((entity, args.Transform)));
    }

    private void OnBodyTypeChanged(Entity<WeightlessnessComponent> entity, ref PhysicsBodyTypeChangedEvent args)
    {
        // No need to update weightlessness if we're not weightless and we're a body type that can't be weightless
        if (args.New is BodyType.Static or BodyType.Kinematic && entity.Comp.Weightless == false)
            return;

        RefreshWeightless((entity.Owner, entity.Comp));
    }

    /// <summary>
    /// Checks if a given entity is currently standing on a grid or map that supports having gravity at all.
    /// </summary>
    public bool EntityOnGravitySupportingGridOrMap(Entity<TransformComponent?> entity)
    {
        entity.Comp ??= Transform(entity);

        return _gravityQuery.HasComp(entity.Comp.GridUid) ||
               _gravityQuery.HasComp(entity.Comp.MapUid);
    }

    /// <summary>
    /// Checks if a given entity is currently standing on a grid or map that has gravity of some kind.
    /// </summary>
    public bool EntityGridOrMapHaveGravity(Entity<TransformComponent?> entity)
    {
        entity.Comp ??= Transform(entity);

        // DO NOT SET TO WEIGHTLESS IF THEY'RE IN NULL-SPACE
        // TODO: If entities actually properly pause when leaving PVS rather than entering null-space this can probably go.
        if (entity.Comp.MapID == MapId.Nullspace)
            return true;

        return _gravityQuery.TryComp(entity.Comp.GridUid, out var gravity) && gravity.Enabled ||
               _gravityQuery.TryComp(entity.Comp.MapUid, out var mapGravity) && mapGravity.Enabled;
    }

    private void OnHandleState(EntityUid uid, GravityComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GravityComponentState state)
            return;

        if (component.EnabledVV == state.Enabled)
            return;
        component.EnabledVV = state.Enabled;
        var ev = new GravityChangedEvent(uid, component.EnabledVV);
        RaiseLocalEvent(uid, ref ev, true);
    }

    private void OnGetState(EntityUid uid, GravityComponent component, ref ComponentGetState args)
    {
        args.State = new GravityComponentState(component.EnabledVV);
    }

    private void OnGravityChange(ref GravityChangedEvent args)
    {
        var gravity = AllEntityQuery<WeightlessnessComponent, TransformComponent>();
        while(gravity.MoveNext(out var uid, out var weightless, out var xform))
        {
            if (xform.GridUid != args.ChangedGridIndex || args.HasGravity == !weightless.Weightless )
                continue;

            ChangeWeightless((uid, weightless));
        }
    }

    private void OnAlertsSync(AlertSyncEvent ev)
    {
        if (IsWeightless(ev.Euid))
            _alerts.ShowAlert(ev.Euid, WeightlessAlert);
        else
            _alerts.ClearAlert(ev.Euid, WeightlessAlert);
    }

    private void OnAlertsParentChange(EntityUid uid, AlertsComponent component, ref EntParentChangedMessage args)
    {
        if (IsWeightless(uid))
            _alerts.ShowAlert(uid, WeightlessAlert);
        else
            _alerts.ClearAlert(uid, WeightlessAlert);
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        EnsureComp<GravityComponent>(ev.EntityUid);
    }

    [Serializable, NetSerializable]
    private sealed class GravityComponentState : ComponentState
    {
        public bool Enabled { get; }

        public GravityComponentState(bool enabled)
        {
            Enabled = enabled;
        }
    }

    private void OnThrowerImpulse(Entity<WeightlessnessComponent> entity, ref ThrowerImpulseEvent args)
    {
        args.Push = true;
    }

    private void OnShooterImpulse(Entity<WeightlessnessComponent> entity, ref ShooterImpulseEvent args)
    {
        args.Push = true;
    }

    private void OnKnockdownAttempt(Entity<WeightlessnessComponent> entity, ref KnockDownAttemptEvent args)
    {
        // Directed, targeted moth attack.
        if (entity.Comp.Weightless)
            args.Cancelled = true;
    }

    private void OnGetStandUpTime(Entity<WeightlessnessComponent> entity, ref GetStandUpTimeEvent args)
    {
        // Get up instantly if weightless
        if (entity.Comp.Weightless)
            args.DoAfterTime = TimeSpan.Zero;
    }
}

/// <summary>
/// Raised to determine if an entity's weightlessness is being overwritten by a component or item with a component.
/// </summary>
/// <param name="IsWeightless">Whether we should be weightless</param>
/// <param name="Handled">Whether something is trying to override our weightlessness</param>
[ByRefEvent]
public record struct IsWeightlessEvent(bool IsWeightless = false, bool Handled = false) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => ~SlotFlags.POCKET;
}

// TODO: Could be funny if going from weightless to weighted sometimes knocked you down...
/// <summary>
/// Raised on an entity when their weightless status changes.
/// </summary>
[ByRefEvent]
public record struct WeightlessnessChangedEvent(bool Weightless);

