using Content.Shared.Actions;
using Content.Shared.Gravity;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class SharedJetpackSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, GetItemActionsEvent>(OnJetpackGetAction);
        SubscribeLocalEvent<JetpackComponent, DroppedEvent>(OnJetpackDropped);
        SubscribeLocalEvent<JetpackComponent, ToggleJetpackEvent>(OnJetpackToggle);

        SubscribeLocalEvent<JetpackUserComponent, RefreshWeightlessModifiersEvent>(OnJetpackUserWeightlessMovement);
        SubscribeLocalEvent<JetpackUserComponent, CanWeightlessMoveEvent>(OnJetpackUserCanWeightless);
        SubscribeLocalEvent<JetpackUserComponent, EntParentChangedMessage>(OnJetpackUserEntParentChanged);
        SubscribeLocalEvent<JetpackComponent, EntGotInsertedIntoContainerMessage>(OnJetpackMoved);

        SubscribeLocalEvent<GravityChangedEvent>(OnJetpackUserGravityChanged);
        SubscribeLocalEvent<JetpackComponent, MapInitEvent>(OnMapInit);
    }

    private void OnJetpackUserWeightlessMovement(Entity<JetpackUserComponent> ent, ref RefreshWeightlessModifiersEvent args)
    {
        // Yes this bulldozes the values but primarily for backwards compat atm.
        args.WeightlessAcceleration = ent.Comp.WeightlessAcceleration;
        args.WeightlessModifier = ent.Comp.WeightlessModifier;
        args.WeightlessFriction = ent.Comp.WeightlessFriction;
        args.WeightlessFrictionNoInput = ent.Comp.WeightlessFrictionNoInput;
    }

    private void OnMapInit(EntityUid uid, JetpackComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        Dirty(uid, component);
    }

    private void OnJetpackUserGravityChanged(ref GravityChangedEvent ev)
    {
        var gridUid = ev.ChangedGridIndex;
        var jetpackQuery = GetEntityQuery<JetpackComponent>();

        var query = EntityQueryEnumerator<JetpackUserComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var user, out var transform))
        {
            if (transform.GridUid == gridUid && ev.HasGravity &&
                jetpackQuery.TryGetComponent(user.Jetpack, out var jetpack))
            {
                _popup.PopupClient(Loc.GetString("jetpack-to-grid"), uid, uid);

                SetEnabled(user.Jetpack, jetpack, false, uid);
            }
        }
    }

    private void OnJetpackDropped(EntityUid uid, JetpackComponent component, DroppedEvent args)
    {
        SetEnabled(uid, component, false, args.User);
    }

    private void OnJetpackMoved(Entity<JetpackComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.Owner != ent.Comp.JetpackUser)
            SetEnabled(ent, ent.Comp, false, ent.Comp.JetpackUser);
    }

    private void OnJetpackUserCanWeightless(EntityUid uid, JetpackUserComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }

    private void OnJetpackUserEntParentChanged(EntityUid uid, JetpackUserComponent component, ref EntParentChangedMessage args)
    {
        if (TryComp<JetpackComponent>(component.Jetpack, out var jetpack) &&
            !CanEnableOnGrid(args.Transform.GridUid))
        {
            SetEnabled(component.Jetpack, jetpack, false, uid);

            _popup.PopupClient(Loc.GetString("jetpack-to-grid"), uid, uid);
        }
    }

    private void SetupUser(EntityUid user, EntityUid jetpackUid, JetpackComponent component)
    {
        EnsureComp<JetpackUserComponent>(user, out var userComp);
        component.JetpackUser = user;

        if (TryComp<PhysicsComponent>(user, out var physics))
            _physics.SetBodyStatus(user, physics, BodyStatus.InAir);

        userComp.Jetpack = jetpackUid;
        userComp.WeightlessAcceleration = component.Acceleration;
        userComp.WeightlessModifier = component.WeightlessModifier;
        userComp.WeightlessFriction = component.Friction;
        userComp.WeightlessFrictionNoInput = component.Friction;
        _movementSpeedModifier.RefreshWeightlessModifiers(user);
    }

    private void RemoveUser(EntityUid uid, JetpackComponent component)
    {
        if (!RemComp<JetpackUserComponent>(uid))
            return;

        component.JetpackUser = null;

        if (TryComp<PhysicsComponent>(uid, out var physics))
            _physics.SetBodyStatus(uid, physics, BodyStatus.OnGround);

        _movementSpeedModifier.RefreshWeightlessModifiers(uid);
    }

    private void OnJetpackToggle(EntityUid uid, JetpackComponent component, ToggleJetpackEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(uid, out TransformComponent? xform) && !CanEnableOnGrid(xform.GridUid))
        {
            _popup.PopupClient(Loc.GetString("jetpack-no-station"), uid, args.Performer);

            return;
        }

        SetEnabled(uid, component, !IsEnabled(uid));
    }

    private bool CanEnableOnGrid(EntityUid? gridUid)
    {
        // No and no again! Do not attempt to activate the jetpack on a grid with gravity disabled. You will not be the first or the last to try this.
        // https://discord.com/channels/310555209753690112/310555209753690112/1270067921682694234
        return gridUid == null ||
               (!HasComp<GravityComponent>(gridUid));
    }

    private void OnJetpackGetAction(EntityUid uid, JetpackComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private bool IsEnabled(EntityUid uid)
    {
        return HasComp<ActiveJetpackComponent>(uid);
    }

    public void SetEnabled(EntityUid uid, JetpackComponent component, bool enabled, EntityUid? user = null)
    {
        if (IsEnabled(uid) == enabled ||
            enabled && !CanEnable(uid, component))
            return;

        if (user == null)
        {
            if (!Container.TryGetContainingContainer((uid, null, null), out var container))
                return;
            user = container.Owner;
        }

        if (enabled)
        {
            SetupUser(user.Value, uid, component);
            EnsureComp<ActiveJetpackComponent>(uid);
        }
        else
        {
            RemoveUser(user.Value, component);
            RemComp<ActiveJetpackComponent>(uid);
        }


        Appearance.SetData(uid, JetpackVisuals.Enabled, enabled);
        Dirty(uid, component);
    }

    public bool IsUserFlying(EntityUid uid)
    {
        return HasComp<JetpackUserComponent>(uid);
    }

    protected virtual bool CanEnable(EntityUid uid, JetpackComponent component)
    {
        return true;
    }
}

[Serializable, NetSerializable]
public enum JetpackVisuals : byte
{
    Enabled,
}
