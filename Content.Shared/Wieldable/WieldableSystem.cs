using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
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
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Wieldable;

public sealed class WieldableSystem : EntitySystem
{
    [Dependency] private readonly SharedVirtualItemSystem _virtualItemSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WieldableComponent, UseInHandEvent>(OnUseInHand, before: [typeof(SharedGunSystem)]);
        SubscribeLocalEvent<WieldableComponent, ItemUnwieldedEvent>(OnItemUnwielded);
        SubscribeLocalEvent<WieldableComponent, GotUnequippedHandEvent>(OnItemLeaveHand);
        SubscribeLocalEvent<WieldableComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<WieldableComponent, GetVerbsEvent<InteractionVerb>>(AddToggleWieldVerb);
        SubscribeLocalEvent<WieldableComponent, HandDeselectedEvent>(OnDeselectWieldable);

        SubscribeLocalEvent<MeleeRequiresWieldComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<GunRequiresWieldComponent, ExaminedEvent>(OnExamineRequires);
        SubscribeLocalEvent<GunRequiresWieldComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<GunWieldBonusComponent, ItemWieldedEvent>(OnGunWielded);
        SubscribeLocalEvent<GunWieldBonusComponent, ItemUnwieldedEvent>(OnGunUnwielded);
        SubscribeLocalEvent<GunWieldBonusComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<GunWieldBonusComponent, ExaminedEvent>(OnExamine);

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
                _popupSystem.PopupClient(message, args.Used, args.User);
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
        if (!component.Wielded ||
            _handsSystem.EnumerateHands(args.User).Count() > 2)
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

    private void OnExamineRequires(Entity<GunRequiresWieldComponent> entity, ref ExaminedEvent args)
    {
        if(entity.Comp.WieldRequiresExamineMessage != null)
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

        if (!component.Wielded)
            args.Handled = TryWield(uid, component, args.User);
        else if (component.UnwieldOnUse)
            args.Handled = TryUnwield(uid, component, args.User);
    }

    public bool CanWield(EntityUid uid, WieldableComponent component, EntityUid user, bool quiet = false)
    {
        // Do they have enough hands free?
        if (!EntityManager.TryGetComponent<HandsComponent>(user, out var hands))
        {
            if (!quiet)
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

        if (_handsSystem.CountFreeableHands((user, hands)) < component.FreeHandsRequired)
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
            _itemSystem.SetHeldPrefix(used, component.WieldedInhandPrefix, component: item);
        }

        component.Wielded = true;

        if (component.WieldSound != null)
            _audioSystem.PlayPredicted(component.WieldSound, used, user);

        //This section handles spawning the virtual item(s) to occupy the required additional hand(s).
        //Since the client can't currently predict entity spawning, only do this if this is running serverside.
        //Remove this check if TrySpawnVirtualItem in SharedVirtualItemSystem is allowed to complete clientside.
        if (_netManager.IsServer)
        {
            var virtuals = new List<EntityUid>();
            for (var i = 0; i < component.FreeHandsRequired; i++)
            {
                if (_virtualItemSystem.TrySpawnVirtualItemInHand(used, user, out var virtualItem, true))
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
        }

        if (TryComp(used, out UseDelayComponent? useDelay)
            && !_delay.TryResetDelay((used, useDelay), true))
            return false;

        var selfMessage = Loc.GetString("wieldable-component-successful-wield", ("item", used));
        var othersMessage = Loc.GetString("wieldable-component-successful-wield-other", ("user", user), ("item", used));
        _popupSystem.PopupPredicted(selfMessage, othersMessage, user, user);

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

        component.Wielded = false;
        var targEv = new ItemUnwieldedEvent(user);

        RaiseLocalEvent(used, targEv);
        return true;
    }

    private void OnItemUnwielded(EntityUid uid, WieldableComponent component, ItemUnwieldedEvent args)
    {
        if (args.User == null)
            return;

        if (TryComp<ItemComponent>(uid, out var item))
        {
            _itemSystem.SetHeldPrefix(uid, component.OldInhandPrefix, component: item);
        }

        if (!args.Force) // don't play sound/popup if this was a forced unwield
        {
            if (component.UnwieldSound != null)
                _audioSystem.PlayPredicted(component.UnwieldSound, uid, args.User);

            var selfMessage = Loc.GetString("wieldable-component-failed-wield", ("item", uid));
            var othersMessage = Loc.GetString("wieldable-component-failed-wield-other", ("user", args.User.Value), ("item", uid));
            _popupSystem.PopupPredicted(selfMessage, othersMessage, args.User.Value, args.User.Value);
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
