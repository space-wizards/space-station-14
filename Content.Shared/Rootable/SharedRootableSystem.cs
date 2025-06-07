using Content.Shared.Damage.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Content.Shared.Fluids.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
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
/// Being rooted prevents weighlessness and slipping, but causes any floor contents to transfer its reagents to the bloodstream.
/// </summary>
public abstract class SharedRootableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    protected EntityQuery<PuddleComponent> PuddleQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        PuddleQuery = GetEntityQuery<PuddleComponent>();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<RootableComponent, MapInitEvent>(OnRootableMapInit);
        SubscribeLocalEvent<RootableComponent, ComponentShutdown>(OnRootableShutdown);
        SubscribeLocalEvent<RootableComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<RootableComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<RootableComponent, ToggleActionEvent>(OnRootableToggle);
        SubscribeLocalEvent<RootableComponent, IsEquippingAttemptEvent>(OnEquippingAttempt);
        SubscribeLocalEvent<RootableComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RootableComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<RootableComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<RootableComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    private void OnRootableMapInit(Entity<RootableComponent> entity, ref MapInitEvent args)
    {
        if (!TryComp(entity, out ActionsComponent? comp))
            return;

        entity.Comp.NextUpdate = _timing.CurTime;
        _actions.AddAction(entity, ref entity.Comp.ActionEntity, entity.Comp.Action, component: comp);
    }

    private void OnRootableShutdown(Entity<RootableComponent> entity, ref ComponentShutdown args)
    {
        if (!TryComp(entity, out ActionsComponent? comp))
            return;

        var actions = new Entity<ActionsComponent?>(entity, comp);
        _actions.RemoveAction(actions, entity.Comp.ActionEntity);
    }

    private void OnRootableToggle(Entity<RootableComponent> entity, ref ToggleActionEvent args)
    {
        args.Handled = TryToggleRooting((entity, entity));
    }

    private void OnEquippingAttempt(Entity<RootableComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.Rooted)
            return;

        if (!args.SlotFlags.HasFlag(ent.Comp.BlockingSlot))
            return;

        args.Reason = Loc.GetString("rootable-popup-fail-equip");
        args.Cancel();
    }

    private void OnMobStateChanged(Entity<RootableComponent> entity, ref MobStateChangedEvent args)
    {
        if (entity.Comp.Rooted)
            TryToggleRooting((entity, entity));
    }

    public bool CanRoot(Entity<RootableComponent> ent, out string reason)
    {
        reason = string.Empty;
        var slotEnumerator = _inventory.GetSlotEnumerator(ent.Owner, ent.Comp.BlockingSlot);
        while (slotEnumerator.NextItem(out var item))
        {
            reason = Loc.GetString("rootable-popup-fail-action", ("blocker", item));
            return false;
        }

        return true;
    }

    public bool TryToggleRooting(Entity<RootableComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!CanRoot((entity, entity.Comp), out var reason))
        {
            _popup.PopupClient(reason, entity.Owner, entity.Owner);
            return false;
        }

        entity.Comp.Rooted = !entity.Comp.Rooted;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(entity);
        Dirty(entity);

        if (entity.Comp.Rooted)
        {
            _alerts.ShowAlert(entity, entity.Comp.RootedAlert);
            var curTime = _timing.CurTime;
            if (curTime > entity.Comp.NextUpdate)
            {
                entity.Comp.NextUpdate = curTime;
            }
        }
        else
        {
            _alerts.ClearAlert(entity, entity.Comp.RootedAlert);
        }

        _audio.PlayPredicted(entity.Comp.RootSound, entity.Owner.ToCoordinates(), entity);

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
        if (!PuddleQuery.HasComp(args.OtherEntity))
            return;

        entity.Comp.PuddleEntity = args.OtherEntity;

        if (entity.Comp.NextUpdate < _timing.CurTime) // To prevent constantly moving to new puddles resetting the timer
            entity.Comp.NextUpdate = _timing.CurTime;
    }

    private void OnEndCollide(Entity<RootableComponent> entity, ref EndCollideEvent args)
    {
        if (entity.Comp.PuddleEntity != args.OtherEntity)
            return;

        var exists = Exists(args.OtherEntity);

        if (!PhysicsQuery.TryComp(entity, out var body))
            return;

        foreach (var ent in _physics.GetContactingEntities(entity, body))
        {
            if (exists && ent == args.OtherEntity)
                continue;

            if (!PuddleQuery.HasComponent(ent))
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
