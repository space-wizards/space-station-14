using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private EntityQuery<BlockingComponent> _blockQuery;

    public override void Initialize()
    {
        base.Initialize();
        InitializeUser();

        _blockQuery = GetEntityQuery<BlockingComponent>();

        SubscribeLocalEvent<BlockingComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<BlockingComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<BlockingComponent, DroppedEvent>(OnDrop);

        SubscribeLocalEvent<BlockingComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<BlockingComponent, ToggleActionEvent>(OnToggleAction);

        SubscribeLocalEvent<BlockingComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<BlockingComponent, GetVerbsEvent<ExamineVerb>>(OnVerbExamine);
        SubscribeLocalEvent<BlockingComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<BlockingComponent> shield, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(shield, ref shield.Comp.BlockingToggleActionEntity, shield.Comp.BlockingToggleAction);
        Dirty(shield);
    }

    private void OnEquip(Entity<BlockingComponent> shield, ref GotEquippedHandEvent args)
    {
        shield.Comp.User = args.User;
        Dirty(shield);

        //To make sure that this bodytype doesn't get set as anything but the original
        if (TryComp<PhysicsComponent>(args.User, out var physicsComponent) && physicsComponent.BodyType != BodyType.Static && !HasComp<BlockingUserComponent>(args.User))
        {
            var userComp = EnsureComp<BlockingUserComponent>(args.User);
            userComp.BlockingItem = shield;
            userComp.OriginalBodyType = physicsComponent.BodyType;
        }
    }

    private void OnUnequip(Entity<BlockingComponent> shield, ref GotUnequippedHandEvent args)
    {
        StopBlockingHelper(shield, args.User);
    }

    private void OnDrop(Entity<BlockingComponent> shield, ref DroppedEvent args)
    {
        StopBlockingHelper(shield, args.User);
    }

    private void OnGetActions(Entity<BlockingComponent> shield, ref GetItemActionsEvent args)
    {
        args.AddAction(ref shield.Comp.BlockingToggleActionEntity, shield.Comp.BlockingToggleAction);
    }

    private void OnToggleAction(Entity<BlockingComponent> shield, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        var handQuery = GetEntityQuery<HandsComponent>();
        if (!handQuery.TryGetComponent(args.Performer, out var hands))
            return;

        var shields = _handsSystem.EnumerateHeld((args.Performer, hands)).ToArray();

        foreach (var heldShield in shields)
        {
            if (heldShield == shield.Owner
                || !_blockQuery.TryGetComponent(heldShield, out var otherBlockComp)
                || !otherBlockComp.IsBlocking)
                continue;

            _popupSystem.PopupClient(Loc.GetString("action-popup-blocking-user-already-blocking"), args.Performer, args.Performer);
            return;
        }

        if (shield.Comp.IsBlocking)
            StopBlocking(shield, args.Performer);
        else
            StartBlocking(shield, args.Performer);

        args.Handled = true;
    }

    private void OnShutdown(Entity<BlockingComponent> shield, ref ComponentShutdown args)
    {
        //In theory the user should not be null when this fires off
        if (shield.Comp.User != null)
        {
            _actionsSystem.RemoveProvidedActions(shield.Comp.User.Value, shield);
            StopBlockingHelper(shield, shield.Comp.User.Value);
        }
    }

    /// <summary>
    /// Called where you want the user to start blocking
    /// Creates a new hard fixture to bodyblock
    /// Also makes the user static to prevent prediction issues
    /// </summary>
    /// <param name="shield"> The shield entity with the blocking component</param>
    /// <param name="user"> The entity who's using the item to block</param>
    /// <returns></returns>
    public bool StartBlocking(Entity<BlockingComponent> shield, EntityUid user)
    {
        if (shield.Comp.IsBlocking)
            return false;

        var xform = Transform(user);

        var shieldName = Name(shield);

        var blockerName = Identity.Entity(user, EntityManager);
        var msgUser = Loc.GetString("action-popup-blocking-user", ("shield", shieldName));
        var msgOther = Loc.GetString("action-popup-blocking-other", ("blockerName", blockerName), ("shield", shieldName));

        //Don't allow someone to block if they're not parented to a grid
        if (xform.GridUid != xform.ParentUid)
        {
            _popupSystem.PopupClient(Loc.GetString("action-popup-blocking-user-cant-block"), user, user);
            return false;
        }

        // Don't allow someone to block if they're not holding the shield
        if (!_handsSystem.IsHolding(user, shield, out _))
        {
            _popupSystem.PopupClient(Loc.GetString("action-popup-blocking-user-not-holding"), user, user);
            return false;
        }

        //Don't allow someone to block if someone else is on the same tile
        var playerTileRef = _turf.GetTileRef(xform.Coordinates);
        if (playerTileRef != null)
        {
            var intersecting = _lookup.GetLocalEntitiesIntersecting(playerTileRef.Value, 0f);
            var mobQuery = GetEntityQuery<MobStateComponent>();
            foreach (var uid in intersecting)
            {
                if (uid != user && mobQuery.HasComponent(uid))
                {
                    _popupSystem.PopupClient(Loc.GetString("action-popup-blocking-user-too-close"), user, user);
                    return false;
                }
            }
        }

        //Don't allow someone to block if they're somehow not anchored.
        _transformSystem.AnchorEntity(user, xform);
        if (!xform.Anchored)
        {
            _popupSystem.PopupClient(Loc.GetString("action-popup-blocking-user-cant-block"), user, user);
            return false;
        }
        _actionsSystem.SetToggled(shield.Comp.BlockingToggleActionEntity, true);
        _popupSystem.PopupPredicted(msgUser, msgOther, user, user);

        if (TryComp<PhysicsComponent>(user, out var physicsComponent))
        {
            _fixtureSystem.TryCreateFixture(user,
                shield.Comp.Shape,
                BlockingComponent.BlockFixtureID,
                hard: true,
                collisionLayer: (int)CollisionGroup.WallLayer,
                body: physicsComponent);
        }

        shield.Comp.IsBlocking = true;
        EnsureComp<HasRaisedShieldComponent>(user);
        Dirty(shield);

        return true;
    }

    /// <summary>
    /// Called where you want the user to stop blocking.
    /// </summary>
    /// <param name="shield"> The shield entity with the blocking component</param>
    /// <param name="user"> The entity who's using the item to block</param>
    /// <returns></returns>
    public bool StopBlocking(Entity<BlockingComponent> shield, EntityUid user)
    {
        if (!shield.Comp.IsBlocking)
            return false;

        var xform = Transform(user);

        var shieldName = Name(shield);

        var blockerName = Identity.Entity(user, EntityManager);
        var msgUser = Loc.GetString("action-popup-blocking-disabling-user", ("shield", shieldName));
        var msgOther = Loc.GetString("action-popup-blocking-disabling-other", ("blockerName", blockerName), ("shield", shieldName));

        //If the component blocking toggle isn't null, grab the users SharedBlockingUserComponent and PhysicsComponent
        //then toggle the action to false, unanchor the user, remove the hard fixture
        //and set the users bodytype back to their original type
        if (TryComp<BlockingUserComponent>(user, out var blockingUserComponent) && TryComp<PhysicsComponent>(user, out var physicsComponent))
        {
            if (xform.Anchored)
                _transformSystem.Unanchor(user, xform, false);

            _actionsSystem.SetToggled(shield.Comp.BlockingToggleActionEntity, false);
            _fixtureSystem.DestroyFixture(user, BlockingComponent.BlockFixtureID, body: physicsComponent);
            _physics.SetBodyType(user, blockingUserComponent.OriginalBodyType, body: physicsComponent);
            _popupSystem.PopupPredicted(msgUser, msgOther, user, user);
        }

        shield.Comp.IsBlocking = false;
        RemComp<HasRaisedShieldComponent>(user);
        Dirty(shield);

        return true;
    }

    /// <summary>
    /// Called where you want someone to stop blocking and to remove the <see cref="BlockingUserComponent"/> from them
    /// Won't remove the <see cref="BlockingUserComponent"/> if they're holding another blocking item
    /// </summary>
    /// <param name="shield"> The shield entity with the blocking component</param>
    /// <param name="user"> The person holding the blocking item </param>
    private void StopBlockingHelper(Entity<BlockingComponent> shield, EntityUid user)
    {
        if (shield.Comp.IsBlocking)
            StopBlocking(shield, user);

        var userQuery = GetEntityQuery<BlockingUserComponent>();
        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(user, out var hands))
            return;

        var shields = _handsSystem.EnumerateHeld((user, hands)).ToArray();

        foreach (var heldShield in shields)
        {
            if (HasComp<BlockingComponent>(heldShield) && userQuery.TryGetComponent(user, out var blockingUserComponent))
            {
                blockingUserComponent.BlockingItem = heldShield;
                return;
            }
        }

        RemComp<BlockingUserComponent>(user);
        shield.Comp.User = null;
    }

    private void OnVerbExamine(Entity<BlockingComponent> shield, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;
        var shieldComp = shield.Comp;
        var fraction = shieldComp.IsBlocking ? shieldComp.ActiveBlockFraction : shieldComp.PassiveBlockFraction;
        var modifier = shieldComp.IsBlocking ? shieldComp.ActiveBlockDamageModifier : shieldComp.PassiveBlockDamageModifer;

        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("blocking-fraction", ("value", MathF.Round(fraction * 100, 1))));

        AppendCoefficients(modifier, msg);

        _examine.AddDetailedExamineVerb(args, shieldComp, msg,
            Loc.GetString("blocking-examinable-verb-text"),
            "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("blocking-examinable-verb-message")
        );
    }

    private void AppendCoefficients(DamageModifierSet modifiers, FormattedMessage msg)
    {
        foreach (var coefficient in modifiers.Coefficients)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Robust.Shared.Localization.Loc.GetString("blocking-coefficient-value",
                ("type", coefficient.Key),
                ("value", MathF.Round(coefficient.Value * 100, 1))
            ));
        }

        foreach (var flat in modifiers.FlatReduction)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Robust.Shared.Localization.Loc.GetString("blocking-reduction-value",
                ("type", flat.Key),
                ("value", flat.Value)
            ));
        }
    }
}
