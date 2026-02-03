using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Shared.Wieldable;

public abstract class SharedWieldableSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WieldableComponent, UseInHandEvent>(OnUseInHand, before: [typeof(SharedGunSystem), typeof(BatteryWeaponFireModesSystem)]);
        SubscribeLocalEvent<WieldableComponent, ItemUnwieldedEvent>(OnItemUnwielded);
        SubscribeLocalEvent<WieldableComponent, GotUnequippedHandEvent>(OnItemLeaveHand);
        SubscribeLocalEvent<WieldableComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<WieldableComponent, GetVerbsEvent<InteractionVerb>>(AddToggleWieldVerb);
        SubscribeLocalEvent<WieldableComponent, HandDeselectedEvent>(OnDeselectWieldable);

        SubscribeLocalEvent<WieldingBlockerComponent, GotEquippedEvent>(OnBlockerEquipped);
        SubscribeLocalEvent<WieldingBlockerComponent, GotEquippedHandEvent>(OnBlockerEquippedHand);
        SubscribeLocalEvent<WieldingBlockerComponent, WieldAttemptEvent>(OnBlockerAttempt);
        SubscribeLocalEvent<WieldingBlockerComponent, InventoryRelayedEvent<WieldAttemptEvent>>(OnBlockerAttempt);
        SubscribeLocalEvent<WieldingBlockerComponent, HeldRelayedEvent<WieldAttemptEvent>>(OnBlockerAttempt);

        SubscribeLocalEvent<MeleeRequiresWieldComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<GunRequiresWieldComponent, ExaminedEvent>(OnExamineRequires);
        SubscribeLocalEvent<GunRequiresWieldComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<GunWieldBonusComponent, ItemWieldedEvent>(OnGunWielded);
        SubscribeLocalEvent<GunWieldBonusComponent, ItemUnwieldedEvent>(OnGunUnwielded);
        SubscribeLocalEvent<GunWieldBonusComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<GunWieldBonusComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SpeedModifiedOnWieldComponent, ItemWieldedEvent>(OnSpeedModifierWielded);
        SubscribeLocalEvent<SpeedModifiedOnWieldComponent, ItemUnwieldedEvent>(OnSpeedModifierUnwielded);
        SubscribeLocalEvent<SpeedModifiedOnWieldComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshSpeedWielded);

        SubscribeLocalEvent<IncreaseDamageOnWieldComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    private void OnMeleeAttempt(EntityUid uid, MeleeRequiresWieldComponent component, ref AttemptMeleeEvent args)
    {
        if (TryComp<WieldableComponent>(uid, out var wieldable) &&
            !wieldable.Wielded)
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("wieldable-component-requires", ("item", uid));
        }
    }

    private void OnShootAttempt(EntityUid uid, GunRequiresWieldComponent component, ref ShotAttemptedEvent args)
    {
        if (TryComp<WieldableComponent>(uid, out var wieldable) &&
            !wieldable.Wielded)
        {
            args.Cancel();

            var time = _timing.CurTime;
            if (time > component.LastPopup + component.PopupCooldown &&
                !HasComp<MeleeWeaponComponent>(uid) &&
                !HasComp<MeleeRequiresWieldComponent>(uid))
            {
                component.LastPopup = time;
                var message = Loc.GetString("wieldable-component-requires", ("item", uid));
                _popup.PopupClient(message, args.Used, args.User);
            }
        }
    }

    private void OnGunUnwielded(EntityUid uid, GunWieldBonusComponent component, ItemUnwieldedEvent args)
    {
        _gun.RefreshModifiers(uid);
    }

    private void OnGunWielded(EntityUid uid, GunWieldBonusComponent component, ref ItemWieldedEvent args)
    {
        _gun.RefreshModifiers(uid);
    }

    private void OnDeselectWieldable(EntityUid uid, WieldableComponent component, HandDeselectedEvent args)
    {
        if (_hands.GetHandCount(args.User) > 2)
            return;

        TryUnwield(uid, component, args.User);
    }

    private void OnGunRefreshModifiers(Entity<GunWieldBonusComponent> bonus, ref GunRefreshModifiersEvent args)
    {
        if (TryComp(bonus, out WieldableComponent? wield) &&
            wield.Wielded)
        {
            args.MinAngle += bonus.Comp.MinAngle;
            args.MaxAngle += bonus.Comp.MaxAngle;
            args.AngleDecay += bonus.Comp.AngleDecay;
            args.AngleIncrease += bonus.Comp.AngleIncrease;
        }
    }

    private void OnSpeedModifierWielded(EntityUid uid, SpeedModifiedOnWieldComponent component, ItemWieldedEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnSpeedModifierUnwielded(EntityUid uid, SpeedModifiedOnWieldComponent component, ItemUnwieldedEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnRefreshSpeedWielded(EntityUid uid, SpeedModifiedOnWieldComponent component, ref HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (TryComp<WieldableComponent>(uid, out var wield) && wield.Wielded)
        {
            args.Args.ModifySpeed(component.WalkModifier, component.SprintModifier);
        }
    }

    private void OnExamineRequires(Entity<GunRequiresWieldComponent> entity, ref ExaminedEvent args)
    {
        if (entity.Comp.WieldRequiresExamineMessage != null)
            args.PushText(Loc.GetString(entity.Comp.WieldRequiresExamineMessage));
    }

    private void OnExamine(EntityUid uid, GunWieldBonusComponent component, ref ExaminedEvent args)
    {
        if (HasComp<GunRequiresWieldComponent>(uid))
            return;

        if (component.WieldBonusExamineMessage != null)
            args.PushText(Loc.GetString(component.WieldBonusExamineMessage));
    }

    private void AddToggleWieldVerb(EntityUid uid, WieldableComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (!_hands.IsHolding((args.User, args.Hands), uid, out _))
            return;

        // TODO VERB TOOLTIPS Make CanWield or some other function return string, set as verb tooltip and disable
        // verb. Or just don't add it to the list if the action is not executable.

        // TODO VERBS ICON
        InteractionVerb verb = new()
        {
            Text = component.Wielded ? Loc.GetString("wieldable-verb-text-unwield") : Loc.GetString("wieldable-verb-text-wield"),
            Act = component.Wielded
                ? () => TryUnwield(uid, component, args.User)
                : () => TryWield(uid, component, args.User)
        };

        args.Verbs.Add(verb);
    }

    private void OnUseInHand(EntityUid uid, WieldableComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Wielded)
        {
            TryWield(uid, component, args.User);
            args.Handled = true; // always mark as handled or we will cycle ammo when wielding is blocked
        }
        else if (component.UnwieldOnUse)
        {
            TryUnwield(uid, component, args.User);
            args.Handled = true;
        }

        if (HasComp<UseDelayComponent>(uid) && !component.UseDelayOnWield)
            args.ApplyDelay = false;
    }

    private void OnBlockerEquipped(Entity<WieldingBlockerComponent> ent, ref GotEquippedEvent args)
    {
        if (ent.Comp.BlockEquipped)
            UnwieldAll(args.Equipee, force: true);
    }

    private void OnBlockerEquippedHand(Entity<WieldingBlockerComponent> ent, ref GotEquippedHandEvent args)
    {
        if (ent.Comp.BlockInHand)
            UnwieldAll(args.User, force: true);
    }

    private void OnBlockerAttempt(Entity<WieldingBlockerComponent> ent, ref InventoryRelayedEvent<WieldAttemptEvent> args)
    {
        if (ent.Comp.BlockEquipped)
        {
            args.Args.Message = Loc.GetString("wieldable-component-blocked-wield", ("blocker", ent.Owner), ("item", args.Args.Wielded));
            args.Args.Cancelled = true;
        }
    }

    private void OnBlockerAttempt(Entity<WieldingBlockerComponent> ent, ref HeldRelayedEvent<WieldAttemptEvent> args)
    {
        if (ent.Comp.BlockInHand)
        {
            args.Args.Message = Loc.GetString("wieldable-component-blocked-wield", ("blocker", ent.Owner), ("item", args.Args.Wielded));
            args.Args.Cancelled = true;
        }
    }

    private void OnBlockerAttempt(Entity<WieldingBlockerComponent> ent, ref WieldAttemptEvent args)
    {
        args.Cancelled = true;
    }

    public bool CanWield(EntityUid uid, WieldableComponent component, EntityUid user, bool quiet = false)
    {
        // Do they have enough hands free?
        if (!TryComp<HandsComponent>(user, out var hands))
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("wieldable-component-no-hands"), user, user);
            return false;
        }

        // Is it.. actually in one of their hands?
        if (!_hands.IsHolding((user, hands), uid, out _))
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("wieldable-component-not-in-hands", ("item", uid)), user, user);
            return false;
        }

        if (_hands.CountFreeableHands((user, hands), except: uid) < component.FreeHandsRequired)
        {
            if (!quiet)
            {
                var message = Loc.GetString("wieldable-component-not-enough-free-hands",
                    ("number", component.FreeHandsRequired), ("item", uid));
                _popup.PopupClient(message, user, user);
            }
            return false;
        }

        // Seems legit.
        return true;
    }

    /// <summary>
    ///     Attempts to wield an item, starting a UseDelay after.
    /// </summary>
    /// <returns>True if the attempt wasn't blocked.</returns>
    public bool TryWield(EntityUid used, WieldableComponent component, EntityUid user)
    {
        if (!CanWield(used, component, user))
            return false;

        if (TryComp(used, out UseDelayComponent? useDelay) && component.UseDelayOnWield)
        {
            if (!_delay.TryResetDelay((used, useDelay), true))
                return false;
        }

        var attemptEv = new WieldAttemptEvent(user, used);
        RaiseLocalEvent(user, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            if (attemptEv.Message != null)
                _popup.PopupClient(attemptEv.Message, user, user);
            return false;
        }

        if (TryComp<ItemComponent>(used, out var item))
        {
            component.OldInhandPrefix = item.HeldPrefix;
            _item.SetHeldPrefix(used, component.WieldedInhandPrefix, component: item);
        }

        SetWielded((used, component), true);

        if (component.WieldSound != null)
            _audio.PlayPredicted(component.WieldSound, used, user);

        //This section handles spawning the virtual item(s) to occupy the required additional hand(s).
        var virtuals = new ValueList<EntityUid>();
        for (var i = 0; i < component.FreeHandsRequired; i++)
        {
            // don't show a popup when dropping items because it will overlap with the popup for wielding
            if (_virtualItem.TrySpawnVirtualItemInHand(used, user, out var virtualItem, true, silent: true))
            {
                virtuals.Add(virtualItem.Value);
                continue;
            }

            foreach (var existingVirtual in virtuals)
            {
                QueueDel(existingVirtual);
            }

            return false;
        }

        var selfMessage = Loc.GetString("wieldable-component-successful-wield", ("item", used));
        var othersMessage = Loc.GetString("wieldable-component-successful-wield-other", ("user", Identity.Entity(user, EntityManager)), ("item", used));
        _popup.PopupPredicted(selfMessage, othersMessage, user, user);

        var ev = new ItemWieldedEvent(user);
        RaiseLocalEvent(used, ref ev);

        return true;
    }

    /// <summary>
    ///     Attempts to unwield an item, with no use delay.
    /// </summary>
    /// <returns>True if the attempt wasn't blocked.</returns>
    public bool TryUnwield(EntityUid used, WieldableComponent component, EntityUid user, bool force = false)
    {
        if (!component.Wielded)
            return false; // already unwielded

        if (!force)
        {
            var attemptEv = new UnwieldAttemptEvent(user, used);
            RaiseLocalEvent(user, ref attemptEv);

            if (attemptEv.Cancelled)
            {
                if (attemptEv.Message != null)
                    _popup.PopupClient(attemptEv.Message, user, user);
                return false;
            }
        }

        SetWielded((used, component), false);

        var ev = new ItemUnwieldedEvent(user, force);
        RaiseLocalEvent(used, ref ev);
        return true;
    }

    /// <summary>
    /// Makes an entity unwield all currently wielded items.
    /// </summary>
    /// <param name="force">If this is true we will bypass UnwieldAttemptEvent.</param>
    public void UnwieldAll(Entity<HandsComponent?> wielder, bool force = false)
    {
        foreach (var held in _hands.EnumerateHeld(wielder))
        {
            if (TryComp<WieldableComponent>(held, out var wieldable))
                TryUnwield(held, wieldable, wielder, force);
        }
    }

    /// <summary>
    /// Sets wielded without doing any checks.
    /// </summary>
    private void SetWielded(Entity<WieldableComponent> ent, bool wielded)
    {
        ent.Comp.Wielded = wielded;
        Dirty(ent);
        _appearance.SetData(ent, WieldableVisuals.Wielded, wielded);
    }

    private void OnItemUnwielded(EntityUid uid, WieldableComponent component, ItemUnwieldedEvent args)
    {
        _item.SetHeldPrefix(uid, component.OldInhandPrefix);

        var user = args.User;
        _virtualItem.DeleteInHandsMatching(user, uid);

        if (!args.Force) // don't play sound/popup if this was a forced unwield
        {
            if (component.UnwieldSound != null)
                _audio.PlayPredicted(component.UnwieldSound, uid, user);

            var selfMessage = Loc.GetString("wieldable-component-failed-wield", ("item", uid));
            var othersMessage = Loc.GetString("wieldable-component-failed-wield-other", ("user", Identity.Entity(args.User, EntityManager)), ("item", uid));
            _popup.PopupPredicted(selfMessage, othersMessage, user, user);
        }
    }

    private void OnItemLeaveHand(EntityUid uid, WieldableComponent component, GotUnequippedHandEvent args)
    {
        if (uid == args.Unequipped)
            TryUnwield(uid, component, args.User, force: true);
    }

    private void OnVirtualItemDeleted(EntityUid uid, WieldableComponent component, VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity == uid)
            TryUnwield(uid, component, args.User, force: true);
    }

    private void OnGetMeleeDamage(EntityUid uid, IncreaseDamageOnWieldComponent component, ref GetMeleeDamageEvent args)
    {
        if (!TryComp<WieldableComponent>(uid, out var wield))
            return;

        if (!wield.Wielded)
            return;

        args.Damage += component.BonusDamage;
    }
}
