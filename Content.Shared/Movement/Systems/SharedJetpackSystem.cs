using Content.Shared.Actions;
using Content.Shared.Gravity;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class SharedJetpackSystem : EntitySystem
{
    [Dependency] private   readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] protected  readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private   readonly SharedMoverController _mover = default!;
    [Dependency] private   readonly SharedPopupSystem _popup = default!;
    [Dependency] private   readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, GetItemActionsEvent>(OnJetpackGetAction);
        SubscribeLocalEvent<JetpackComponent, DroppedEvent>(OnJetpackDropped);
        SubscribeLocalEvent<JetpackComponent, ToggleJetpackEvent>(OnJetpackToggle);
        SubscribeLocalEvent<JetpackComponent, CanWeightlessMoveEvent>(OnJetpackCanWeightlessMove);

        SubscribeLocalEvent<JetpackUserComponent, CanWeightlessMoveEvent>(OnJetpackUserCanWeightless);
        SubscribeLocalEvent<JetpackUserComponent, EntParentChangedMessage>(OnJetpackUserEntParentChanged);
        SubscribeLocalEvent<JetpackUserComponent, ComponentGetState>(OnJetpackUserGetState);
        SubscribeLocalEvent<JetpackUserComponent, ComponentHandleState>(OnJetpackUserHandleState);

        SubscribeLocalEvent<GravityChangedEvent>(OnJetpackUserGravityChanged);
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
                _popup.PopupClient(Loc.GetString("jetpack-to-grid"), uid, uid);

                SetEnabled(user.Jetpack, jetpack, false, uid);
            }
        }
    }

    private void OnJetpackUserHandleState(EntityUid uid, JetpackUserComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not JetpackUserComponentState state)
            return;

        component.Jetpack = EnsureEntity<JetpackUserComponent>(state.Jetpack, uid);
    }

    private void OnJetpackUserGetState(EntityUid uid, JetpackUserComponent component, ref ComponentGetState args)
    {
        args.State = new JetpackUserComponentState()
        {
            Jetpack = GetNetEntity(component.Jetpack),
        };
    }

    private void OnJetpackDropped(EntityUid uid, JetpackComponent component, DroppedEvent args)
    {
        SetEnabled(uid, component, false, args.User);
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

    private void SetupUser(EntityUid user, EntityUid jetpackUid)
    {
        var userComp = EnsureComp<JetpackUserComponent>(user);
        _mover.SetRelay(user, jetpackUid);

        if (TryComp<PhysicsComponent>(user, out var physics))
            _physics.SetBodyStatus(physics, BodyStatus.InAir);

        userComp.Jetpack = jetpackUid;
    }

    private void RemoveUser(EntityUid uid)
    {
        if (!RemComp<JetpackUserComponent>(uid))
            return;

        if (TryComp<PhysicsComponent>(uid, out var physics))
            _physics.SetBodyStatus(physics, BodyStatus.OnGround);

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

        SetEnabled(uid, component, !IsEnabled(uid));
    }

    private bool CanEnableOnGrid(EntityUid? gridUid)
    {
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
        {
            return;
        }

        if (enabled)
        {
            EnsureComp<ActiveJetpackComponent>(uid);
        }
        else
        {
            RemComp<ActiveJetpackComponent>(uid);
        }

        if (user == null)
        {
            Container.TryGetContainingContainer(uid, out var container);
            user = container?.Owner;
        }

        // Can't activate if no one's using.
        if (user == null && enabled)
            return;

        if (user != null)
        {
            if (enabled)
            {
                SetupUser(user.Value, uid);
            }
            else
            {
                RemoveUser(user.Value);
            }

            _movementSpeedModifier.RefreshMovementSpeedModifiers(user.Value);
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

    [Serializable, NetSerializable]
    protected sealed class JetpackUserComponentState : ComponentState
    {
        public NetEntity Jetpack;
    }
}

[Serializable, NetSerializable]
public enum JetpackVisuals : byte
{
    Enabled,
}
