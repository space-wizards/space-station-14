using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
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

    protected EntityQuery<GravityComponent> GravityQuery;
    private EntityQuery<GravityAffectedComponent> _weightlessQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        // Grid Gravity
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<GravityChangedEvent>(OnGravityChange);

        // Weightlessness
        SubscribeLocalEvent<GravityAffectedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GravityAffectedComponent, EntParentChangedMessage>(OnEntParentChanged);
        SubscribeLocalEvent<GravityAffectedComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChanged);

        // Alerts
        SubscribeLocalEvent<AlertSyncEvent>(OnAlertsSync);
        SubscribeLocalEvent<AlertsComponent, WeightlessnessChangedEvent>(OnWeightlessnessChanged);
        SubscribeLocalEvent<AlertsComponent, EntParentChangedMessage>(OnAlertsParentChange);

        // Impulse
        SubscribeLocalEvent<GravityAffectedComponent, ShooterImpulseEvent>(OnShooterImpulse);
        SubscribeLocalEvent<GravityAffectedComponent, ThrowerImpulseEvent>(OnThrowerImpulse);

        GravityQuery = GetEntityQuery<GravityComponent>();
        _weightlessQuery = GetEntityQuery<GravityAffectedComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateShake();
    }

    public bool IsWeightless(Entity<GravityAffectedComponent?> entity)
    {
        // If we can be weightless and are weightless, return true, otherwise return false
        return _weightlessQuery.Resolve(entity, ref entity.Comp, false) && entity.Comp.Weightless;
    }

    private bool GetWeightless(Entity<GravityAffectedComponent, PhysicsComponent?> entity)
    {
        if (!_physicsQuery.Resolve(entity, ref entity.Comp2, false))
            return false;

        if (entity.Comp2.BodyType is BodyType.Static or BodyType.Kinematic)
            return false;

        // Check if something other than the grid or map is overriding our gravity
        var ev = new IsWeightlessEvent();
        RaiseLocalEvent(entity, ref ev);
        if (ev.Handled)
            return ev.IsWeightless;

        return !EntityGridOrMapHaveGravity(entity.Owner);
    }

    /// <summary>
    /// Refreshes weightlessness status, needs to be called anytime it would change.
    /// </summary>
    /// <param name="entity">The entity we are updating the weightless status of</param>
    public void RefreshWeightless(Entity<GravityAffectedComponent?> entity)
    {
        if (!_weightlessQuery.Resolve(entity, ref entity.Comp))
            return;

        UpdateWeightless(entity!);
    }

    /// <summary>
    /// Overload of <see cref="RefreshWeightless(Entity{GravityAffectedComponent?})"/> which also takes a bool for the weightlessness value we want to change to.
    /// This method should only be called if there is no chance something can override the weightless value you're trying to change to.
    /// This is really only the case if you're applying a weightless value that overrides non-conditionally from events or are a grid with the gravity component.
    /// </summary>
    /// <param name="entity">The entity we are updating the weightless status of</param>
    /// <param name="weightless">The weightless value we are trying to change to, helps avoid needless networking</param>
    public void RefreshWeightless(Entity<GravityAffectedComponent?> entity, bool weightless)
    {
        if (!_weightlessQuery.Resolve(entity, ref entity.Comp))
            return;

        // Only update if we're changing our weightless status
        if (entity.Comp.Weightless == weightless)
            return;

        UpdateWeightless(entity!);
    }

    private void UpdateWeightless(Entity<GravityAffectedComponent> entity)
    {
        var newWeightless = GetWeightless(entity);

        // Don't network or raise events if it's not changing
        if (newWeightless == entity.Comp.Weightless)
            return;

        entity.Comp.Weightless = newWeightless;
        Dirty(entity);

        var ev = new WeightlessnessChangedEvent(entity.Comp.Weightless);
        RaiseLocalEvent(entity, ref ev);
    }

    private void OnMapInit(Entity<GravityAffectedComponent> entity, ref MapInitEvent args)
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

    private void OnEntParentChanged(Entity<GravityAffectedComponent> entity, ref EntParentChangedMessage args)
    {
        // If we've moved but are still on the same grid, then don't do anything.
        if (args.OldParent == args.Transform.GridUid)
            return;

        RefreshWeightless((entity.Owner, entity.Comp));
    }

    private void OnBodyTypeChanged(Entity<GravityAffectedComponent> entity, ref PhysicsBodyTypeChangedEvent args)
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

        return GravityQuery.HasComp(entity.Comp.GridUid) ||
               GravityQuery.HasComp(entity.Comp.MapUid);
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

        return GravityQuery.TryComp(entity.Comp.GridUid, out var gravity) && gravity.Enabled ||
               GravityQuery.TryComp(entity.Comp.MapUid, out var mapGravity) && mapGravity.Enabled;
    }

    private void OnGravityChange(ref GravityChangedEvent args)
    {
        var gravity = AllEntityQuery<GravityAffectedComponent, TransformComponent>();
        while(gravity.MoveNext(out var uid, out var weightless, out var xform))
        {
            if (xform.GridUid != args.ChangedGridIndex)
                continue;

            RefreshWeightless((uid, weightless), !args.HasGravity);
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

    private void OnThrowerImpulse(Entity<GravityAffectedComponent> entity, ref ThrowerImpulseEvent args)
    {
        args.Push = true;
    }

    private void OnShooterImpulse(Entity<GravityAffectedComponent> entity, ref ShooterImpulseEvent args)
    {
        args.Push = true;
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

/// <summary>
/// Raised on an entity when their weightless status changes.
/// </summary>
[ByRefEvent]
public readonly record struct WeightlessnessChangedEvent(bool Weightless);
