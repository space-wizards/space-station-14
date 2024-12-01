using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
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
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
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
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WieldableComponent, UseInHandEvent>(OnUseInHand, before: [typeof(SharedGunSystem)]);
        SubscribeLocalEvent<WieldableComponent, GotUnequippedHandEvent>(OnItemLeaveHand);
        SubscribeLocalEvent<WieldableComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<WieldableComponent, GetVerbsEvent<InteractionVerb>>(AddToggleWieldVerb);
        SubscribeLocalEvent<WieldableComponent, HandDeselectedEvent>(OnDeselectWieldable);

        SubscribeLocalEvent<MeleeRequiresWieldComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<IncreaseDamageOnWieldComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);

        SubscribeLocalEvent<GunRequiresWieldComponent, ExaminedEvent>(OnExamineRequires);
        SubscribeLocalEvent<GunRequiresWieldComponent, ShotAttemptedEvent>(OnShootAttempt);

        SubscribeLocalEvent<GunWieldBonusComponent, ItemWieldedEvent>(OnGunWielded);
        SubscribeLocalEvent<GunWieldBonusComponent, ItemUnwieldedEvent>(OnGunUnwielded);
        SubscribeLocalEvent<GunWieldBonusComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<GunWieldBonusComponent, ExaminedEvent>(OnExamine);
    }

    /// <summary>
    /// Attempt to attack with a gun that requires being wielded to melee attack
    /// </summary>
    /// <param name="entity">The gun that requires wielding to melee</param>
    /// <param name="args">Attack attempt event</param>
    private void OnMeleeAttempt(Entity<MeleeRequiresWieldComponent> entity, ref AttemptMeleeEvent args)
    {
        if (TryComp<WieldableComponent>(entity, out var wieldable) &&
            !wieldable.Wielded)
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("wieldable-component-requires", ("item", entity));
        }
    }

    /// <summary>
    /// Adds bonus damage to the gun when wielded
    /// </summary>
    /// <param name="entity">The gun that receives a bonus to melee damage when wielded</param>
    /// <param name="args">Melee damage retrieval event</param>
    private void OnGetMeleeDamage(Entity<IncreaseDamageOnWieldComponent> entity, ref GetMeleeDamageEvent args)
    {
        if (!TryComp<WieldableComponent>(entity, out var wieldableComponent))
            return;

        if (!wieldableComponent.Wielded)
            return;

        args.Damage += entity.Comp.BonusDamage;
    }

    /// <summary>
    /// Attempt to shoot with a gun that requires being wielded to shoot
    /// </summary>
    /// <param name="entity">The gun that requires wielding to shoot</param>
    /// <param name="args">Shot attempt event</param>
    private void OnShootAttempt(Entity<GunRequiresWieldComponent> entity, ref ShotAttemptedEvent args)
    {
        if (TryComp<WieldableComponent>(entity, out var wieldable) &&
            !wieldable.Wielded)
        {
            args.Cancel();

            var time = _timing.CurTime;
            if (time > entity.Comp.LastPopup + entity.Comp.PopupCooldown &&
                !HasComp<MeleeWeaponComponent>(entity) &&
                !HasComp<MeleeRequiresWieldComponent>(entity))
            {
                entity.Comp.LastPopup = time;
                var message = Loc.GetString("wieldable-component-requires", ("item", entity));
                _popupSystem.PopupClient(message, args.Used, args.User);
            }
        }
    }

    /// <summary>
    /// Updates the gun's aiming bonuses when the gun is unwielded
    /// </summary>
    /// <param name="entity">The gun being unwielded</param>
    /// <param name="args">The unwield event</param>
    private void OnGunUnwielded(Entity<GunWieldBonusComponent> entity, ref ItemUnwieldedEvent args)
    {
        if (TryComp<GunComponent>(entity, out var gunComponent))
            _gun.RefreshModifiers((entity, gunComponent));
    }

    /// <summary>
    /// Updates the gun's aiming bonuses when the gun is wielded
    /// </summary>
    /// <param name="entity">The gun being wielded</param>
    /// <param name="args">The wield event</param>
    private void OnGunWielded(Entity<GunWieldBonusComponent> entity, ref ItemWieldedEvent args)
    {
        if (TryComp<GunComponent>(entity, out var gunComponent))
            _gun.RefreshModifiers((entity, gunComponent));
    }

    /// <summary>
    /// The gun's modifiers actually being changed when wielded
    /// </summary>
    /// <param name="entity">The entity containing the bonuses that the gun should receive</param>
    /// <param name="args">The modifier event</param>
    private void OnGunRefreshModifiers(Entity<GunWieldBonusComponent> entity, ref GunRefreshModifiersEvent args)
    {
        if (TryComp(entity, out WieldableComponent? wield) &&
            wield.Wielded)
        {
            args.MinAngle += entity.Comp.MinAngle;
            args.MaxAngle += entity.Comp.MaxAngle;
            args.AngleDecay += entity.Comp.AngleDecay;
            args.AngleIncrease += entity.Comp.AngleIncrease;
        }
    }

    /// <summary>
    /// Attempt to automatically unwield the weapon when the gun is deselected
    /// </summary>
    /// <param name="entity">The gun being unwielded</param>
    /// <param name="args">The hand deslection event</param>
    private void OnDeselectWieldable(Entity<WieldableComponent> entity, ref HandDeselectedEvent args)
    {
        if (!entity.Comp.Wielded ||
            _handsSystem.EnumerateHands(args.User).Count() > 2)
            return;

        TryUnwield(entity, args.User);
    }

    /// <summary>
    /// Notifies the user in the examine text that the gun requires wielding to shoot
    /// </summary>
    /// <param name="entity">The gun that requires wielding</param>
    /// <param name="args">The examine event on the gun</param>
    private void OnExamineRequires(Entity<GunRequiresWieldComponent> entity, ref ExaminedEvent args)
    {
        if(entity.Comp.WieldRequiresExamineMessage != null)
            args.PushText(Loc.GetString(entity.Comp.WieldRequiresExamineMessage));
    }

    /// <summary>
    /// Notifies the user in the examine text that the gun fires more accurately when wielded
    /// </summary>
    /// <param name="entity">The gun that gains a bonus when being wielded</param>
    /// <param name="args">The examine event on the gun</param>
    private void OnExamine(Entity<GunWieldBonusComponent> entity, ref ExaminedEvent args)
    {
        if (HasComp<GunRequiresWieldComponent>(entity))
            return;

        if (entity.Comp.WieldBonusExamineMessage != null)
            args.PushText(Loc.GetString(entity.Comp.WieldBonusExamineMessage));
    }

    /// <summary>
    /// Adds the wield or unwield verb to the verb menu
    /// </summary>
    /// <param name="entity">The gun that can be wielded</param>
    /// <param name="args">The event that retrieves verb options for the entity</param>
    private void AddToggleWieldVerb(Entity<WieldableComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (!_handsSystem.IsHolding(args.User, entity, out _, args.Hands))
            return;

        // TODO VERB TOOLTIPS Make CanWield or some other function return string, set as verb tooltip and disable
        // verb. Or just don't add it to the list if the action is not executable.

        // TODO VERBS ICON
        var user = args.User;
        InteractionVerb verb = new()
        {
            Text = entity.Comp.Wielded ? Loc.GetString("wieldable-verb-text-unwield") : Loc.GetString("wieldable-verb-text-wield"),
            Act = entity.Comp.Wielded
                ? () => TryUnwield(entity, user)
                : () => TryWield(entity,user),
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Using the gun in hand either causes the gun to be wielded
    /// but also potentially can unwield the gun if entity.Comp.UnwieldOnUse is true
    /// </summary>
    /// <param name="entity">The gun that can be wielded</param>
    /// <param name="args">Event thrown when using the gun</param>
    private void OnUseInHand(Entity<WieldableComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!entity.Comp.Wielded)
            args.Handled = TryWield(entity, args.User);
        else if (entity.Comp.UnwieldOnUse)
            args.Handled = TryUnwield(entity, args.User);
    }

    /// <summary>
    /// Checks if the user has enough free hands to wield the gun
    /// </summary>
    /// <param name="entity">The gun that can be wielded</param>
    /// <param name="user">The user holding the gun</param>
    /// <param name="quiet">When set to true, hides the popup messages that indicate a lack of hands</param>
    /// <returns></returns>
    private bool CanWield(Entity<WieldableComponent> entity, EntityUid user, bool quiet = false)
    {
        // Do they have enough hands free?
        if (!EntityManager.TryGetComponent<HandsComponent>(user, out var hands))
        {
            if (!quiet)
                _popupSystem.PopupClient(Loc.GetString("wieldable-component-no-hands"), user, user);
            return false;
        }

        // Is it... actually in one of their hands?
        if (!_handsSystem.IsHolding(user, entity, out _, hands))
        {
            if (!quiet)
                _popupSystem.PopupClient(Loc.GetString("wieldable-component-not-in-hands", ("item", entity)), user, user);
            return false;
        }

        if (_handsSystem.CountFreeableHands((user, hands)) < entity.Comp.FreeHandsRequired)
        {
            if (!quiet)
            {
                var message = Loc.GetString("wieldable-component-not-enough-free-hands",
                    ("number", entity.Comp.FreeHandsRequired), ("item", entity));
                _popupSystem.PopupClient(message, user, user);
            }
            return false;
        }

        // Seems legit.
        return true;
    }

    /// <summary>
    /// Attempts to wield an item, starting a UseDelay after.
    /// </summary>
    /// <param name="entity">The gun that can be wielded</param>
    /// <param name="user">The user holding the gun</param>
    /// <returns>True if the attempt wasn't blocked.</returns>
    private bool TryWield(Entity<WieldableComponent> entity, EntityUid user)
    {
        if (!CanWield(entity, user))
            return false;

        var ev = new BeforeWieldEvent();
        RaiseLocalEvent(entity, ev);

        if (ev.Cancelled)
            return false;

        var targEv = new ItemWieldedEvent();
        RaiseLocalEvent(entity, ref targEv);

        if (TryComp<ItemComponent>(entity, out var itemComponent))
        {
            entity.Comp.OldInhandPrefix = itemComponent.HeldPrefix;
            _itemSystem.SetHeldPrefix(entity, entity.Comp.WieldedInhandPrefix, component: itemComponent);
        }

        entity.Comp.Wielded = true;
        Dirty(entity, entity.Comp);

        if (entity.Comp.WieldSound != null)
            _audioSystem.PlayPredicted(entity.Comp.WieldSound, entity, user);

        //This section handles spawning the virtual item(s) to occupy the required additional hand(s).
        //Since the client can't currently predict entity spawning, only do this if this is running serverside.
        //Remove this check if TrySpawnVirtualItem in SharedVirtualItemSystem is allowed to complete clientside.
        if (_netManager.IsServer)
        {
            var virtuals = new List<EntityUid>();
            for (var i = 0; i < entity.Comp.FreeHandsRequired; i++)
            {
                if (_virtualItemSystem.TrySpawnVirtualItemInHand(entity, user, out var virtualItem, true))
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

        if (TryComp(entity, out UseDelayComponent? useDelay)
            && !_delay.TryResetDelay((entity, useDelay), true))
            return false;

        var selfMessage = Loc.GetString("wieldable-component-successful-wield", ("item", entity));
        var othersMessage = Loc.GetString("wieldable-component-successful-wield-other", ("user", Identity.Entity(user, EntityManager)), ("item", entity));
        _popupSystem.PopupClient(selfMessage, user, user);
        _popupSystem.PopupEntity(othersMessage,
            user,
            Filter.PvsExcept(user, entityManager: EntityManager)
                .RemoveWhere(e =>
                    e.AttachedEntity != null && _container.IsEntityInContainer(user)
                                             && !_container.IsInSameOrParentContainer(
                                                 (user, Transform(user)),
                                                 (e.AttachedEntity.Value, Transform(e.AttachedEntity.Value)))),
            true);

        return true;
    }
    /// <summary>
    /// Attempts to unwield an item, with no DoAfter.
    /// </summary>
    /// <param name="entity">The gun that can be wielded</param>
    /// <param name="user">The user holding the gun</param>
    /// <param name="force">Whether the gun was forced out of the user's hands, hides the unwield popup when true</param>
    /// <returns>True if the attempt wasn't blocked.</returns>
    private bool TryUnwield(Entity<WieldableComponent> entity, EntityUid user, bool force = false)
    {
        var ev = new BeforeUnwieldEvent();
        RaiseLocalEvent(entity, ev);

        if (ev.Cancelled)
            return false;

        // Throw unwielded event to update gun modifiers
        var targEv = new ItemUnwieldedEvent();
        RaiseLocalEvent(entity, targEv);

        if (TryComp<ItemComponent>(entity, out var itemComponent))
        {
            _itemSystem.SetHeldPrefix(entity, entity.Comp.OldInhandPrefix, component: itemComponent);
        }

        if (!force) // don't play sound/popup if this was a forced unwield
        {
            if (entity.Comp.UnwieldSound != null)
                _audioSystem.PlayPredicted(entity.Comp.UnwieldSound, entity, user);

            var selfMessage = Loc.GetString("wieldable-component-failed-wield", ("item", entity));
            var othersMessage = Loc.GetString("wieldable-component-failed-wield-other", ("user", Identity.Entity(user, EntityManager)), ("item", entity));
            _popupSystem.PopupClient(selfMessage, user, user);
            _popupSystem.PopupEntity(othersMessage,
                user,
                Filter.PvsExcept(user, entityManager: EntityManager)
                    .RemoveWhere(e =>
                        e.AttachedEntity != null && _container.IsEntityInContainer(user)
                                                 && !_container.IsInSameOrParentContainer(
                                                     (user, Transform(user)),
                                                     (e.AttachedEntity.Value, Transform(e.AttachedEntity.Value)))),
                true);
        }

        _appearance.SetData(entity, WieldableVisuals.Wielded, false);
        entity.Comp.Wielded = false;
        Dirty(entity, entity.Comp);
        _virtualItemSystem.DeleteInHandsMatching(user, entity);
        return true;
    }

    /// <summary>
    /// Unwields the gun if the item leaves the hand forcefully, such as being thrown or disarmed or stripped
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void OnItemLeaveHand(Entity<WieldableComponent> entity, ref GotUnequippedHandEvent args)
    {
        //
        if (!entity.Comp.Wielded || entity.Owner != args.Unequipped)
            return;

        TryUnwield(entity, args.User, true);
    }

    /// <summary>
    /// Attempts to unwield the gun when the virtual wield entity is deleted from the hand slot
    /// such as by using the drop key on the virtual entity or when unwielding the gun
    /// </summary>
    /// <param name="entity">The gun being unwielded</param>
    /// <param name="args">Event thrown when virtual item gets deleted from the hands</param>
    private void OnVirtualItemDeleted(Entity<WieldableComponent> entity, ref VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity != entity.Owner || !entity.Comp.Wielded)
            return;

        TryUnwield(entity, args.User);
    }
}
