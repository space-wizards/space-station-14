using Content.Shared.Damage.Components;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Content.Shared.Fluids.Components;
using Content.Shared.Gravity;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Slippery;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Rootable;

/// <summary>
/// Adds an action to toggle rooting to the ground, primarily for the Diona species.
/// </summary>
public abstract class SharedRootableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private EntityQuery<PuddleComponent> _puddleQuery;

    public override void Initialize()
    {
        base.Initialize();

        _puddleQuery = GetEntityQuery<PuddleComponent>();

        SubscribeLocalEvent<RootableComponent, MapInitEvent>(OnRootableMapInit);
        SubscribeLocalEvent<RootableComponent, ComponentShutdown>(OnRootableShutdown);
        SubscribeLocalEvent<RootableComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<RootableComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<RootableComponent, ToggleActionEvent>(OnRootableToggle);
        SubscribeLocalEvent<RootableComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RootableComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<RootableComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<RootableComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    private void OnRootableMapInit(EntityUid uid, RootableComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
    }

    private void OnRootableShutdown(EntityUid uid, RootableComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    private void OnRootableToggle(EntityUid uid, RootableComponent component, ref ToggleActionEvent args)
    {
        args.Handled = TryToggleRooting(uid, rooted: component);
    }

    private void OnMobStateChanged(EntityUid uid, RootableComponent component, MobStateChangedEvent args)
    {
        if (component.Rooted)
            TryToggleRooting(uid, rooted: component);
    }

    public bool TryToggleRooting(EntityUid uid, RootableComponent? rooted = null)
    {
        if (!Resolve(uid, ref rooted))
            return false;

        rooted.Rooted = !rooted.Rooted;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        Dirty(uid, rooted);

        if (rooted.Rooted)
            _alerts.ShowAlert(uid, rooted.RootedAlert);
        else
            _alerts.ClearAlert(uid, rooted.RootedAlert);

        _audioSystem.PlayPredicted(rooted.RootSound, uid.ToCoordinates(), uid);

        return true;
    }

    private void OnIsWeightless(Entity<RootableComponent> ent, ref IsWeightlessEvent args)
    {
        if (args.Handled || !ent.Comp.Rooted)
            return;

        // do not cancel weightlessness if the person is in off-grid.
        if (!_gravity.EntityOnGravitySupportingGridOrMap(ent.Owner))
            return;

        args.IsWeightless = false;
        args.Handled = true;
    }

    private void OnSlipAttempt(Entity<RootableComponent> ent, ref SlipAttemptEvent args)
    {
        if (!ent.Comp.Rooted)
            return;

        if (args.SlipCausingEntity != null && HasComp<DamageUserOnTriggerComponent>(args.SlipCausingEntity))
            return;

        args.NoSlip = true;
    }

    private void OnStartCollide(Entity<RootableComponent> entity, ref StartCollideEvent args)
    {
        if (!_entityManager.HasComponent<PuddleComponent>(args.OtherEntity))
        {
            return;
        }

        entity.Comp.PuddleEntity = args.OtherEntity;

        if (entity.Comp.NextSecond < _timing.CurTime) // To prevent constantly moving to new puddles resetting the timer
            entity.Comp.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);
    }

    private void OnEndCollide(Entity<RootableComponent> entity, ref EndCollideEvent args)
    {
        if (entity.Comp.PuddleEntity != args.OtherEntity)
            return;

        var exists = Exists(args.OtherEntity);

        if (!TryComp<PhysicsComponent>(entity, out var body))
            return;

        foreach (var ent in _physics.GetContactingEntities(entity, body))
        {
            if (exists && ent == args.OtherEntity)
                continue;

            if (!_puddleQuery.HasComponent(ent))
                continue;

            entity.Comp.PuddleEntity = ent;
            return; // New puddle found, no need to continue
        }

        entity.Comp.PuddleEntity = null;
    }

    private void OnRefreshMovementSpeed(Entity<RootableComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (entity.Comp.Rooted)
            args.ModifySpeed(entity.Comp.SpeedModifier);
    }
}
