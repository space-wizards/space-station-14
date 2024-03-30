using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
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
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, GetItemActionsEvent>(OnJetpackGetAction);
        SubscribeLocalEvent<JetpackComponent, DroppedEvent>(OnJetpackDropped);
        SubscribeLocalEvent<JetpackComponent, ToggleJetpackEvent>(OnJetpackToggle);
        SubscribeLocalEvent<JetpackComponent, CanWeightlessMoveEvent>(OnJetpackCanWeightlessMove);

        SubscribeLocalEvent<JetpackUserComponent, CanWeightlessMoveEvent>(OnJetpackUserCanWeightless);
        SubscribeLocalEvent<JetpackUserComponent, EntParentChangedMessage>(OnJetpackUserEntParentChanged);

        SubscribeLocalEvent<GravityChangedEvent>(OnJetpackUserGravityChanged);
        SubscribeLocalEvent<JetpackComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, JetpackComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        Dirty(uid, component);
    }

    private void OnJetpackCanWeightlessMove(EntityUid uid, JetpackComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
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
                DisableJetpack(user.Jetpack, jetpack, uid);
            }
        }
    }

    private void OnJetpackDropped(EntityUid uid, JetpackComponent component, DroppedEvent args)
    {
        DisableJetpack(uid, component, args.User);
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
            DisableJetpack(component.Jetpack, jetpack, uid);
        }
    }

    private void SetupUser(EntityUid user, EntityUid jetpackUid)
    {
        var userComp = EnsureComp<JetpackUserComponent>(user);
        _mover.SetRelay(user, jetpackUid);

        if (TryComp<PhysicsComponent>(user, out var physics))
            _physics.SetBodyStatus(user, physics, BodyStatus.InAir);

        userComp.Jetpack = jetpackUid;
    }

    private void RemoveUser(EntityUid uid)
    {
        if (!RemComp<JetpackUserComponent>(uid))
            return;

        if (TryComp<PhysicsComponent>(uid, out var physics))
            _physics.SetBodyStatus(uid, physics, BodyStatus.OnGround);

        RemComp<RelayInputMoverComponent>(uid);
    }

    private void OnJetpackToggle(EntityUid uid, JetpackComponent component, ToggleJetpackEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<TransformComponent>(uid, out var xform) && !CanEnableOnGrid(xform.GridUid))
        {
            _popup.PopupClient(Loc.GetString("jetpack-no-station"), uid, args.Performer);
            return;
        }

        if (IsEnabled(uid))
            DisableJetpack(uid, component);
        else
            EnableJetpack(uid, component);
    }

    private bool CanEnableOnGrid(EntityUid? gridUid)
    {
        return gridUid == null || !HasComp<GravityComponent>(gridUid);
    }

    private void OnJetpackGetAction(EntityUid uid, JetpackComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private bool IsEnabled(EntityUid uid)
    {
        return HasComp<ActiveJetpackComponent>(uid);
    }

    private EntityUid? TryGetJetpackOwner(EntityUid uid)
    {
        Container.TryGetContainingContainer(uid, out var container);
        return container?.Owner;
    }

    protected void EnableJetpack(EntityUid uid, JetpackComponent component, EntityUid? user = null)
    {
        user ??= TryGetJetpackOwner(uid);

        if (user != null && !IsEnabled(uid) && CanEnable(uid, user.Value, component))
        {
            EnsureComp<ActiveJetpackComponent>(uid);
            SetupUser(user.Value, uid);
            _movementSpeedModifier.RefreshMovementSpeedModifiers(user.Value);
            Appearance.SetData(uid, JetpackVisuals.Enabled, true);
            Dirty(uid, component);
        }
    }

    protected void DisableJetpack(EntityUid uid, JetpackComponent component, EntityUid? user = null)
    {
        if (!IsEnabled(uid))
            return;

        RemComp<ActiveJetpackComponent>(uid);

        user ??= TryGetJetpackOwner(uid);

        if (user != null)
        {
            _popup.PopupClient(Loc.GetString("jetpack-disabled"), uid, user.Value);

            RemoveUser(user.Value);
            _movementSpeedModifier.RefreshMovementSpeedModifiers(user.Value);
        }

        Appearance.SetData(uid, JetpackVisuals.Enabled, false);
        Dirty(uid, component);
    }

    public bool IsUserFlying(EntityUid uid)
    {
        return HasComp<JetpackUserComponent>(uid);
    }

    protected virtual bool CanEnable(EntityUid uid, EntityUid user, JetpackComponent component)
    {
        return true;
    }
}

[Serializable, NetSerializable]
public enum JetpackVisuals : byte
{
    Enabled,
}
