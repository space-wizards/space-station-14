using System.Diagnostics.CodeAnalysis;
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

    private EntityQuery<JetpackUserComponent> _jetpackUserQuery;
    private EntityQuery<ActiveJetpackComponent> _activeJetpackQuery;

    public override void Initialize()
    {
        base.Initialize();

        _jetpackUserQuery = GetEntityQuery<JetpackUserComponent>();
        _activeJetpackQuery = GetEntityQuery<ActiveJetpackComponent>();

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
        if (!IsUserFlying(ent))
            return;

        // Yes this bulldozes the values but primarily for backwards compat atm.
        args.WeightlessAcceleration = ent.Comp.WeightlessAcceleration;
        args.WeightlessModifier = ent.Comp.WeightlessModifier;
        args.WeightlessFriction = ent.Comp.WeightlessFriction;
        args.WeightlessFrictionNoInput = ent.Comp.WeightlessFrictionNoInput;
    }

    private void OnMapInit(Entity<JetpackComponent> jetpack, ref MapInitEvent args)
    {
        var (_, jetpackComponent) = jetpack;

        _actionContainer.EnsureAction(jetpack.Owner, ref jetpackComponent.ToggleActionEntity, jetpackComponent.ToggleAction);
        Dirty(jetpack);
    }

    private void OnJetpackUserGravityChanged(ref GravityChangedEvent ev)
    {
        var gridUid = ev.ChangedGridIndex;
        var jetpackQuery = GetEntityQuery<JetpackComponent>();

        var query = EntityQueryEnumerator<JetpackUserComponent>();
        while (query.MoveNext(out var uid, out var user))
        {
            var transform = Transform(uid);

            var jetpackUid = user.Jetpack;
            if (transform.GridUid == gridUid && jetpackQuery.TryGetComponent(jetpackUid, out var jetpackComponent))
            {
                var jetpack = new Entity<JetpackComponent>(jetpackUid, jetpackComponent);

                var canFly = CanFlyOnGrid(gridUid);
                SetEnabled(jetpack, jetpackComponent.Enabled, flyIfEnabled: canFly, user: uid);

                if (!canFly)
                    _popup.PopupClient(Loc.GetString("jetpack-to-grid"), uid, uid);
            }
        }
    }

    private void OnJetpackDropped(Entity<JetpackComponent> jetpack, ref DroppedEvent args)
        => SetEnabled(jetpack, false, flyIfEnabled: false, user: args.User);

    private void OnJetpackMoved(Entity<JetpackComponent> jetpack, ref EntGotInsertedIntoContainerMessage args)
    {
        var jetpackUser = jetpack.Comp.JetpackUser;
        if (args.Container.Owner != jetpackUser)
            SetEnabled(jetpack, false, flyIfEnabled: false, user: jetpackUser);
    }

    private void OnJetpackUserCanWeightless(Entity<JetpackUserComponent> ent, ref CanWeightlessMoveEvent args) => args.CanMove = true;

    private void OnJetpackUserEntParentChanged(Entity<JetpackUserComponent> ent, ref EntParentChangedMessage args)
    {
        var (userUid, _) = ent;
        var jetpackUid = ent.Comp.Jetpack;

        if (!TryComp<JetpackComponent>(jetpackUid, out var jetpackComponent))
            return;

        var userGrid = Transform(userUid).GridUid;

        var canFly = CanFlyOnGrid(userGrid);
        var jetpackEnabled = jetpackComponent.Enabled;

        if (!canFly && jetpackEnabled)
            _popup.PopupClient(Loc.GetString("jetpack-to-grid"), userUid, userUid);

        SetEnabled(new Entity<JetpackComponent>(jetpackUid, jetpackComponent), jetpackEnabled, flyIfEnabled: canFly, user: userUid);
    }


    private JetpackUserComponent SetupUser(EntityUid user, Entity<JetpackComponent> jetpack)
    {
        var (jetpackUid, jetpackComp) = jetpack;
        EnsureComp<JetpackUserComponent>(user, out var userComp);

        jetpackComp.JetpackUser = user;
        userComp.Jetpack = jetpackUid;

        return userComp;
    }

    private void RemoveUser(EntityUid user, Entity<JetpackComponent> jetpack)
    {
        if (!RemComp<JetpackUserComponent>(user))
            return;

        jetpack.Comp.JetpackUser = null;
    }

    private void StartUserFlying(EntityUid user, Entity<JetpackComponent> jetpack, JetpackUserComponent userComp)
    {
        var (jetpackUid, jetpackComp) = jetpack;
        if (TryComp<PhysicsComponent>(user, out var physics))
            _physics.SetBodyStatus(user, physics, BodyStatus.InAir);

        userComp.Jetpack = jetpackUid;
        userComp.WeightlessAcceleration = jetpackComp.Acceleration;
        userComp.WeightlessModifier = jetpackComp.WeightlessModifier;
        userComp.WeightlessFriction = jetpackComp.Friction;
        userComp.WeightlessFrictionNoInput = jetpackComp.Friction;
        _movementSpeedModifier.RefreshWeightlessModifiers(user);

        EnsureComp<ActiveJetpackComponent>(jetpackUid);
    }

    private void EndUserFlying(EntityUid user, Entity<JetpackComponent> jetpack)
    {
        RemComp<ActiveJetpackComponent>(jetpack.Owner);
        if (TryComp<PhysicsComponent>(user, out var physics))
            _physics.SetBodyStatus(user, physics, BodyStatus.OnGround);

        _movementSpeedModifier.RefreshWeightlessModifiers(user);
    }

    private void OnJetpackToggle(Entity<JetpackComponent> jetpack, ref ToggleJetpackEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        var (uid, component) = jetpack;
        var toggled = !component.Enabled;

        // If they're already using a jetpack that isn't this one and trying to turn this one on, don't let them.
        if (TryComp<JetpackUserComponent>(user, out var userComponent) && userComponent.Jetpack != uid)
        {
            _popup.PopupClient(Loc.GetString("jetpack-already-using"), user);
            return;
        }

        // You can still turn the jetpack on/off when on a grid that doesn't permit flying; you just won't be able to fly!
        var canFly = TryComp(uid, out TransformComponent? xform) && CanFlyOnGrid(xform.GridUid);
        SetEnabled(jetpack, toggled, null, canFly);

        args.Handled = true;
    }

    /// <remarks>
    /// This should return only whether you can <i>fly</i> with the jetpack, assuming it's turned on etc.
    /// Return value should stay regardless of <see cref="CanEnable"/>.
    /// </remarks>
    private bool CanFlyOnGrid(EntityUid? gridUid)
    {
        // No and no again! Do not attempt to activate the jetpack on a grid with gravity disabled. You will not be the first or the last to try this.
        // https://discord.com/channels/310555209753690112/310555209753690112/1270067921682694234
        return gridUid == null || !HasComp<GravityComponent>(gridUid);
    }

    private void OnJetpackGetAction(EntityUid uid, JetpackComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    /// <summary>
    /// Sets the provided jetpack to whether it is enabled, and if so, whether it can fly.
    /// <paramref name="enabled"/> is only whether the jetpack should appear active, but
    /// <paramref name="flyIfEnabled"/> is whether the user should actually be able to fly with it.
    /// </summary>
    /// <param name="enabled">Whether the jetpack should appear active, but not necessarily let the user fly.</param>
    /// <param name="flyIfEnabled">Whether the jetpack should let the user fly, if <paramref name="enabled"/> is true. If null, defaults to whether the user is already flying.</param>
    public void SetEnabled(Entity<JetpackComponent> jetpack, bool enabled, EntityUid? user = null, bool? flyIfEnabled = false)
    {
        var (uid, component) = jetpack;
        if (enabled == component.Enabled && flyIfEnabled == IsFlying(uid))
            return;

        if (user == null)
        {
            if (!Container.TryGetContainingContainer((uid, null, null), out var container))
                return;

            user = container.Owner;
        }

        component.Enabled = enabled;

        // flyIfEnabled defaults to false, but if null it will be just whether the user is already flying.
        // But, that only applies if the jetpack is enabled.
        flyIfEnabled ??= IsUserFlying(user.Value);
        if (enabled)
        {
            var userComp = SetupUser(user.Value, jetpack);
            if (flyIfEnabled.Value)
                StartUserFlying(user.Value, jetpack, userComp);
            else
                EndUserFlying(user.Value, jetpack);
        }
        else
        {
            RemoveUser(user.Value, jetpack);
            if (IsFlying(uid))
                EndUserFlying(user.Value, jetpack);
        }

        Appearance.SetData(uid, JetpackVisuals.Enabled, enabled);
        Dirty(jetpack);
    }


    /// <returns>Whether the provided entity is an active, flying jetpack.</returns>
    /// <remarks>
    /// Analogous to <c>HasComp&lt;<see cref="ActiveJetpackComponent"/>&gt;(<paramref name="uid"/>)</c>, however uses a dedicated <see cref="EntityQuery"/>.
    /// </remarks>
    private bool IsFlying(EntityUid uid)
        => _activeJetpackQuery.HasComp(uid);

    /// <returns>Whether the provided entity is using an enabled jetpack, but isn't necessarily flying with it.</returns>
    /// <remarks>
    /// Analogous to <c>HasComp&lt;<see cref="JetpackUserComponent"/>&gt;(<paramref name="uid"/>)</c>, however uses a dedicated <see cref="EntityQuery"/>.
    /// </remarks>
    public bool IsJetpackUser(EntityUid uid)
        => _jetpackUserQuery.HasComp(uid);

    /// <summary>
    /// Given an entity, tries to get the <see cref="ActiveJetpackComponent"/> of the jetpack, if present, that the entity is using.
    /// By extension, also returns whether the provided entity is flying with an enabled jetpack.
    /// </summary>
    public bool TryGetActiveJetpack(EntityUid uid, [NotNullWhen(true)] out ActiveJetpackComponent? activeJetpackComponent)
    {
        if (!_jetpackUserQuery.TryComp(uid, out var jetpackUserComponent))
        {
            activeJetpackComponent = null;
            return false;
        }

        return TryComp(jetpackUserComponent.Jetpack, out activeJetpackComponent);
    }

    /// <returns>Whether the provided entity is using an active jetpack to fly.</returns>
    public bool IsUserFlying(EntityUid uid)
        => _jetpackUserQuery.TryComp(uid, out var jetpackUserComponent) && _activeJetpackQuery.TryComp(jetpackUserComponent.Jetpack, out _);

    /// <remarks>
    /// This should return only whether you can <i>turn the jetpack on/off</i>, such as according to whether it has enough fuel to work,
    /// not necessarily meaning being able to fly with it.
    /// Return value should stay regardless of <see cref="CanFlyOnGrid"/>.
    /// </remarks>
    protected virtual bool CanEnable(Entity<JetpackComponent> jetpack)
        => true;
}

[Serializable, NetSerializable]
public enum JetpackVisuals : byte
{
    Enabled,
    Layer
}
