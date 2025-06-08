using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player; // imp
using Robust.Shared.Timing; // imp
using Robust.Shared.Utility;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!; // imp
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!; // imp

    public override void Initialize()
    {
        base.Initialize();
        InitializeUser();

        SubscribeLocalEvent<BlockingComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<BlockingComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<BlockingComponent, DroppedEvent>(OnDrop);

        SubscribeLocalEvent<BlockingComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<BlockingComponent, ToggleActionEvent>(OnToggleAction);

        SubscribeLocalEvent<BlockingComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<BlockingComponent, GetVerbsEvent<ExamineVerb>>(OnVerbExamine);
        SubscribeLocalEvent<BlockingComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<BlockingUserComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed); // imp
    }

    private void OnMapInit(EntityUid uid, BlockingComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.BlockingToggleActionEntity, component.BlockingToggleAction);
        Dirty(uid, component);
    }

    private void OnEquip(EntityUid uid, BlockingComponent component, GotEquippedHandEvent args)
    {
        component.User = args.User;
        Dirty(uid, component);

        //To make sure that this bodytype doesn't get set as anything but the original
        if (TryComp<PhysicsComponent>(args.User, out var physicsComponent) && physicsComponent.BodyType != BodyType.Static && !HasComp<BlockingUserComponent>(args.User))
        {
            var userComp = EnsureComp<BlockingUserComponent>(args.User);
            userComp.BlockingItem = uid;
            userComp.OriginalBodyType = physicsComponent.BodyType;
        }
    }

    private void OnUnequip(EntityUid uid, BlockingComponent component, GotUnequippedHandEvent args)
    {
        StopBlockingHelper(uid, component, args.User);
    }

    private void OnDrop(EntityUid uid, BlockingComponent component, DroppedEvent args)
    {
        StopBlockingHelper(uid, component, args.User);
    }

    private void OnGetActions(EntityUid uid, BlockingComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.BlockingToggleActionEntity, component.BlockingToggleAction);
    }

    private void OnToggleAction(EntityUid uid, BlockingComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        var blockQuery = GetEntityQuery<BlockingComponent>();
        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(args.Performer, out var hands))
            return;

        var shields = _handsSystem.EnumerateHeld(args.Performer, hands).ToArray();

        foreach (var shield in shields)
        {
            if (shield == uid)
                continue;

            if (blockQuery.TryGetComponent(shield, out var otherBlockComp) && otherBlockComp.IsBlocking)
            {
                CantBlockError(args.Performer);
                return;
            }
        }

        if (component.IsBlocking)
            StopBlocking((uid, component), args.Performer); // imp. changed to Entity<T>
        else
            StartBlocking((uid, component), args.Performer); // imp. ditto

        args.Handled = true;
    }

    private void OnShutdown(EntityUid uid, BlockingComponent component, ComponentShutdown args)
    {
        //In theory the user should not be null when this fires off
        if (component.User != null)
        {
            _actionsSystem.RemoveProvidedActions(component.User.Value, uid);
            StopBlockingHelper(uid, component, component.User.Value);
        }
    }

    // imp - redid this whole thing to remove the anchoring part and replace it with a movement speed modifier.
    public void StartBlocking(Entity<BlockingComponent> ent, EntityUid user)
    {
        if (ent.Comp.IsBlocking)
            return;

        var shieldName = Name(ent);

        var blockerName = Identity.Entity(user, EntityManager);
        var msgUser = Loc.GetString("action-popup-blocking-user", ("shield", shieldName));
        var msgOther = Loc.GetString("action-popup-blocking-other", ("blockerName", blockerName), ("shield", shieldName));

        // Don't allow someone to block if they're not holding the shield
        if (!_handsSystem.IsHolding(user, ent, out _))
        {
            CantBlockError(user);
            return;
        }

        _actionsSystem.SetToggled(ent.Comp.BlockingToggleActionEntity, true);
        if (_gameTiming.IsFirstTimePredicted)
        {
            _popupSystem.PopupEntity(msgOther, user, Filter.PvsExcept(user), true);
            if (_gameTiming.InPrediction)
                _popupSystem.PopupEntity(msgUser, user, user);
        }

        ent.Comp.IsBlocking = true;
        Dirty(ent);

        _movementSpeed.RefreshMovementSpeedModifiers(user);

        return;
    }

    private void CantBlockError(EntityUid user)
    {
        var msgError = Loc.GetString("action-popup-blocking-user-cant-block");
        _popupSystem.PopupClient(msgError, user, user);
    }

    // imp - changed this whole thing to remove fixtures/anchoring and replace with slowdown
    public void StopBlocking(Entity<BlockingComponent> ent, EntityUid user)
    {
        if (!ent.Comp.IsBlocking)
            return;

        var shieldName = Name(ent);

        var blockerName = Identity.Entity(user, EntityManager);
        var msgUser = Loc.GetString("action-popup-blocking-disabling-user", ("shield", shieldName));
        var msgOther = Loc.GetString("action-popup-blocking-disabling-other", ("blockerName", blockerName), ("shield", shieldName));

        if (TryComp<BlockingUserComponent>(user, out _))
        {
            _actionsSystem.SetToggled(ent.Comp.BlockingToggleActionEntity, false);
            if (_gameTiming.IsFirstTimePredicted)
            {
                _popupSystem.PopupEntity(msgOther, user, Filter.PvsExcept(user), true);
                if (_gameTiming.InPrediction)
                    _popupSystem.PopupEntity(msgUser, user, user);
            }
        }

        ent.Comp.IsBlocking = false;
        Dirty(ent);

        _movementSpeed.RefreshMovementSpeedModifiers(user);

        return;
    }

    // imp - necessary for movement speed modifier
    private void OnRefreshMovespeed(Entity<BlockingUserComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp<BlockingComponent>(ent.Comp.BlockingItem, out var blockingComp))
            return;

        if (blockingComp.IsBlocking)
            args.ModifySpeed(blockingComp.SlowdownModifier);
        else
            args.ModifySpeed(1f);
    }

    /// <summary>
    /// Called where you want someone to stop blocking and to remove the <see cref="BlockingUserComponent"/> from them
    /// Won't remove the <see cref="BlockingUserComponent"/> if they're holding another blocking item
    /// </summary>
    /// <param name="uid"> The item the component is attached to</param>
    /// <param name="component"> The <see cref="BlockingComponent"/> </param>
    /// <param name="user"> The person holding the blocking item </param>
    private void StopBlockingHelper(EntityUid uid, BlockingComponent component, EntityUid user)
    {
        if (component.IsBlocking)
            StopBlocking((uid, component), user); // imp - switched to Entity<T>

        var userQuery = GetEntityQuery<BlockingUserComponent>();
        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(user, out var hands))
            return;

        var shields = _handsSystem.EnumerateHeld(user, hands).ToArray();

        foreach (var shield in shields)
        {
            if (HasComp<BlockingComponent>(shield) && userQuery.TryGetComponent(user, out var blockingUserComponent))
            {
                blockingUserComponent.BlockingItem = shield;
                return;
            }
        }

        RemComp<BlockingUserComponent>(user);
        component.User = null;
    }

    private void OnVerbExamine(EntityUid uid, BlockingComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var fraction = component.IsBlocking ? component.ActiveBlockFraction : component.PassiveBlockFraction;
        var modifier = component.IsBlocking ? component.ActiveBlockDamageModifier : component.PassiveBlockDamageModifer;

        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("blocking-fraction", ("value", MathF.Round(fraction * 100, 1))));

        AppendCoefficients(modifier, msg);

        _examine.AddDetailedExamineVerb(args, component, msg,
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
