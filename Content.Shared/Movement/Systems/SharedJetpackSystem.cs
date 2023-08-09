using Content.Shared.Actions;
using Content.Shared.Gravity;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems;

public abstract class SharedJetpackSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _network = default!;
    [Dependency] protected readonly MovementSpeedModifierSystem MovementSpeedModifier = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;

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

        foreach (var (user, transform) in EntityQuery<JetpackUserComponent, TransformComponent>(true))
        {
            if (transform.GridUid == gridUid && ev.HasGravity &&
                jetpackQuery.TryGetComponent(user.Jetpack, out var jetpack))
            {
                if (_timing.IsFirstTimePredicted)
                    _popups.PopupEntity(Loc.GetString("jetpack-to-grid"), user.Jetpack, user.Owner);

                SetEnabled(jetpack, false, user.Owner);
            }
        }
    }

    private void OnJetpackUserHandleState(EntityUid uid, JetpackUserComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not JetpackUserComponentState state) return;
        component.Jetpack = state.Jetpack;
    }

    private void OnJetpackUserGetState(EntityUid uid, JetpackUserComponent component, ref ComponentGetState args)
    {
        args.State = new JetpackUserComponentState()
        {
            Jetpack = component.Jetpack,
        };
    }

    private void OnJetpackDropped(EntityUid uid, JetpackComponent component, DroppedEvent args)
    {
        SetEnabled(component, false, args.User);
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
            SetEnabled(jetpack, false, uid);

            if (_timing.IsFirstTimePredicted && _network.IsClient)
                _popups.PopupEntity(Loc.GetString("jetpack-to-grid"), uid, uid);
        }
    }

    private void SetupUser(EntityUid uid, JetpackComponent component)
    {
        var user = EnsureComp<JetpackUserComponent>(uid);
        var relay = EnsureComp<RelayInputMoverComponent>(uid);
        _mover.SetRelay(uid, component.Owner, relay);
        user.Jetpack = component.Owner;
    }

    private void RemoveUser(EntityUid uid)
    {
        if (!RemComp<JetpackUserComponent>(uid)) return;
        RemComp<RelayInputMoverComponent>(uid);
    }

    private void OnJetpackToggle(EntityUid uid, JetpackComponent component, ToggleJetpackEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<TransformComponent>(uid, out var xform) && !CanEnableOnGrid(xform.GridUid))
        {
            if (_timing.IsFirstTimePredicted)
                _popups.PopupEntity(Loc.GetString("jetpack-no-station"), uid, args.Performer);

            return;
        }

        SetEnabled(component, !IsEnabled(uid));
    }

    private bool CanEnableOnGrid(EntityUid? gridUid)
    {
        return gridUid == null ||
               (!HasComp<GravityComponent>(gridUid));
    }

    private void OnJetpackGetAction(EntityUid uid, JetpackComponent component, GetItemActionsEvent args)
    {
        args.Actions.Add(component.ToggleAction);
    }

    private bool IsEnabled(EntityUid uid)
    {
        return HasComp<ActiveJetpackComponent>(uid);
    }

    public void SetEnabled(JetpackComponent component, bool enabled, EntityUid? user = null)
    {
        if (IsEnabled(component.Owner) == enabled ||
            enabled && !CanEnable(component)) return;

        if (enabled)
        {
            EnsureComp<ActiveJetpackComponent>(component.Owner);
        }
        else
        {
            RemComp<ActiveJetpackComponent>(component.Owner);
        }

        if (user == null)
        {
            Container.TryGetContainingContainer(component.Owner, out var container);
            user = container?.Owner;
        }

        // Can't activate if no one's using.
        if (user == null && enabled) return;

        if (user != null)
        {
            if (enabled)
            {
                SetupUser(user.Value, component);
            }
            else
            {
                RemoveUser(user.Value);
            }

            MovementSpeedModifier.RefreshMovementSpeedModifiers(user.Value);
        }

        Appearance.SetData(component.Owner, JetpackVisuals.Enabled, enabled);
        Dirty(component);
    }

    public bool IsUserFlying(EntityUid uid)
    {
        return HasComp<JetpackUserComponent>(uid);
    }

    protected virtual bool CanEnable(JetpackComponent component)
    {
        return true;
    }

    [Serializable, NetSerializable]
    protected sealed class JetpackUserComponentState : ComponentState
    {
        public EntityUid Jetpack;
    }
}

[Serializable, NetSerializable]
public enum JetpackVisuals : byte
{
    Enabled,
}
