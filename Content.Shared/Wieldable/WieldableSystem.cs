using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Player;
using Content.Shared.Timing;

namespace Content.Shared.Wieldable;

public sealed class WieldableSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandVirtualItemSystem _virtualItemSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WieldableComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WieldableComponent, ItemUnwieldedEvent>(OnItemUnwielded);
        SubscribeLocalEvent<WieldableComponent, GotUnequippedHandEvent>(OnItemLeaveHand);
        SubscribeLocalEvent<WieldableComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<WieldableComponent, GetVerbsEvent<InteractionVerb>>(AddToggleWieldVerb);

        SubscribeLocalEvent<MeleeRequiresWieldComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<GunRequiresWieldComponent, AttemptShootEvent>(OnShootAttempt);
        SubscribeLocalEvent<GunWieldBonusComponent, ItemWieldedEvent>(OnGunWielded);
        SubscribeLocalEvent<GunWieldBonusComponent, ItemUnwieldedEvent>(OnGunUnwielded);

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

    private void OnShootAttempt(EntityUid uid, GunRequiresWieldComponent component, ref AttemptShootEvent args)
    {
        if (TryComp<WieldableComponent>(uid, out var wieldable) &&
            !wieldable.Wielded)
        {
            args.Cancelled = true;

            if (!HasComp<MeleeWeaponComponent>(uid) && !HasComp<MeleeRequiresWieldComponent>(uid))
            {
                args.Message = Loc.GetString("wieldable-component-requires", ("item", uid));
            }
        }
    }

    private void OnGunUnwielded(EntityUid uid, GunWieldBonusComponent component, ItemUnwieldedEvent args)
    {
        if (!TryComp<GunComponent>(uid, out var gun))
            return;

        gun.MinAngle -= component.MinAngle;
        gun.MaxAngle -= component.MaxAngle;
        Dirty(uid, gun);
    }

    private void OnGunWielded(EntityUid uid, GunWieldBonusComponent component, ref ItemWieldedEvent args)
    {
        if (!TryComp<GunComponent>(uid, out var gun))
            return;

        gun.MinAngle += component.MinAngle;
        gun.MaxAngle += component.MaxAngle;
        Dirty(uid, gun);
    }

    private void AddToggleWieldVerb(EntityUid uid, WieldableComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (!_handsSystem.IsHolding(args.User, uid, out _, args.Hands))
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

        if(!component.Wielded)
            args.Handled = TryWield(uid, component, args.User);
        else
            args.Handled = TryUnwield(uid, component, args.User);
    }

    public bool CanWield(EntityUid uid, WieldableComponent component, EntityUid user, bool quiet=false)
    {
        // Do they have enough hands free?
        if (!EntityManager.TryGetComponent<HandsComponent>(user, out var hands))
        {
            if(!quiet)
                _popupSystem.PopupClient(Loc.GetString("wieldable-component-no-hands"), user, user);
            return false;
        }

        // Is it.. actually in one of their hands?
        if (!_handsSystem.IsHolding(user, uid, out _, hands))
        {
            if (!quiet)
                _popupSystem.PopupClient(Loc.GetString("wieldable-component-not-in-hands", ("item", uid)), user, user);
            return false;
        }

        if (hands.CountFreeHands() < component.FreeHandsRequired)
        {
            if (!quiet)
            {
                var message = Loc.GetString("wieldable-component-not-enough-free-hands",
                    ("number", component.FreeHandsRequired), ("item", uid));
                _popupSystem.PopupClient(message, user, user);
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

        var ev = new BeforeWieldEvent();
        RaiseLocalEvent(used, ev);

        if (ev.Cancelled)
            return false;

        if (TryComp<ItemComponent>(used, out var item))
        {
            component.OldInhandPrefix = item.HeldPrefix;
            _itemSystem.SetHeldPrefix(used, component.WieldedInhandPrefix, item);
        }

        component.Wielded = true;

        if (component.WieldSound != null)
            _audioSystem.PlayPredicted(component.WieldSound, used, user);

        for (var i = 0; i < component.FreeHandsRequired; i++)
        {
            _virtualItemSystem.TrySpawnVirtualItemInHand(used, user);
        }

        _delay.BeginDelay(used);

        _popupSystem.PopupClient(Loc.GetString("wieldable-component-successful-wield", ("item", used)), user, user);
        _popupSystem.PopupEntity(Loc.GetString("wieldable-component-successful-wield-other", ("user", user),("item", used)), user, Filter.PvsExcept(user), true);

        var targEv = new ItemWieldedEvent();
        RaiseLocalEvent(used, ref targEv);

        Dirty(used, component);
        return true;
    }

    /// <summary>
    ///     Attempts to unwield an item, with no DoAfter.
    /// </summary>
    /// <returns>True if the attempt wasn't blocked.</returns>
    public bool TryUnwield(EntityUid used, WieldableComponent component, EntityUid user)
    {
        var ev = new BeforeUnwieldEvent();
        RaiseLocalEvent(used, ev);

        if (ev.Cancelled)
            return false;

        var targEv = new ItemUnwieldedEvent(user);

        RaiseLocalEvent(used, targEv);
        return true;
    }

    private void OnItemUnwielded(EntityUid uid, WieldableComponent component, ItemUnwieldedEvent args)
    {
        if (args.User == null)
            return;
        if (!component.Wielded)
            return;

        if (TryComp<ItemComponent>(uid, out var item))
        {
            _itemSystem.SetHeldPrefix(uid, component.OldInhandPrefix, item);
        }

        component.Wielded = false;

        if (!args.Force) // don't play sound/popup if this was a forced unwield
        {
            if (component.UnwieldSound != null)
                _audioSystem.PlayPredicted(component.UnwieldSound, uid, args.User);

            _popupSystem.PopupClient(Loc.GetString("wieldable-component-failed-wield",
                ("item", uid)), args.User.Value, args.User.Value);
            _popupSystem.PopupEntity(Loc.GetString("wieldable-component-failed-wield-other",
                ("user", args.User.Value), ("item", uid)), args.User.Value, Filter.PvsExcept(args.User.Value), true);
        }

        _appearance.SetData(uid, WieldableVisuals.Wielded, false);

        Dirty(uid, component);
        _virtualItemSystem.DeleteInHandsMatching(args.User.Value, uid);
    }

    private void OnItemLeaveHand(EntityUid uid, WieldableComponent component, GotUnequippedHandEvent args)
    {
        if (!component.Wielded || uid != args.Unequipped)
            return;
        RaiseLocalEvent(uid, new ItemUnwieldedEvent(args.User, force: true), true);
    }

    private void OnVirtualItemDeleted(EntityUid uid, WieldableComponent component, VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity == uid && component.Wielded)
            TryUnwield(args.BlockingEntity, component, args.User);
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
