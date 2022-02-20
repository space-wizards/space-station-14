using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Server.Weapon.Melee;
using Content.Server.Wieldable.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Wieldable
{
    public sealed class WieldableSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WieldableComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<WieldableComponent, ItemWieldedEvent>(OnItemWielded);
            SubscribeLocalEvent<WieldableComponent, ItemUnwieldedEvent>(OnItemUnwielded);
            SubscribeLocalEvent<WieldableComponent, UnequippedHandEvent>(OnItemLeaveHand);
            SubscribeLocalEvent<WieldableComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<WieldableComponent, GetVerbsEvent<InteractionVerb>>(AddToggleWieldVerb);

            SubscribeLocalEvent<IncreaseDamageOnWieldComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void AddToggleWieldVerb(EntityUid uid, WieldableComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            // TODO VERB TOOLTIPS Make CanWield or some other function return string, set as verb tooltip and disable
            // verb. Or just don't add it to the list if the action is not executable.

            // TODO VERBS ICON + localization
            InteractionVerb verb = new()
            {
                Text = component.Wielded ? "Unwield" : "Wield",
                Act = component.Wielded
                    ? () => AttemptUnwield(component.Owner, component, args.User)
                    : () => AttemptWield(component.Owner, component, args.User)
            };

            args.Verbs.Add(verb);
        }

        private void OnUseInHand(EntityUid uid, WieldableComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;
            if(!component.Wielded)
                AttemptWield(uid, component, args.User);
            else
                AttemptUnwield(uid, component, args.User);
        }

        public bool CanWield(EntityUid uid, WieldableComponent component, EntityUid user, bool quiet=false)
        {
            // Do they have enough hands free?
            if (!EntityManager.TryGetComponent<HandsComponent>(user, out var hands))
            {
                if(!quiet)
                    user.PopupMessage(Loc.GetString("wieldable-component-no-hands"));
                return false;
            }

            if (hands.GetFreeHands() < component.FreeHandsRequired)
            {
                // TODO FLUENT need function to change 'hands' to 'hand' when there's only 1 required
                if (!quiet)
                {
                    user.PopupMessage(Loc.GetString("wieldable-component-not-enough-free-hands",
                        ("number", component.FreeHandsRequired),
                        ("item", uid)));
                }

                return false;
            }

            // Is it.. actually in one of their hands?
            if (!hands.TryGetHandHoldingEntity(uid, out _))
            {
                if (!quiet)
                {
                    user.PopupMessage(Loc.GetString("wieldable-component-not-in-hands", ("item", uid)));
                }

                return false;
            }

            // Seems legit.
            return true;
        }

        /// <summary>
        ///     Attempts to wield an item, creating a DoAfter..
        /// </summary>
        public void AttemptWield(EntityUid used, WieldableComponent component, EntityUid user)
        {
            if (!CanWield(used, component, user))
                return;
            var ev = new BeforeWieldEvent();
            RaiseLocalEvent(used, ev, false);

            if (ev.Cancelled) return;

            var doargs = new DoAfterEventArgs(
                user,
                component.WieldTime,
                default,
                used)
            {
                BreakOnUserMove = false,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                TargetFinishedEvent = new ItemWieldedEvent(user),
                UserFinishedEvent = new WieldedItemEvent(used)
            };

            _doAfter.DoAfter(doargs);
        }

        /// <summary>
        ///     Attempts to unwield an item, with no DoAfter.
        /// </summary>
        public void AttemptUnwield(EntityUid used, WieldableComponent component, EntityUid user)
        {
            var ev = new BeforeUnwieldEvent();
            RaiseLocalEvent(used, ev, false);

            if (ev.Cancelled) return;

            var targEv = new ItemUnwieldedEvent(user);
            var userEv = new UnwieldedItemEvent(used);

            RaiseLocalEvent(used, targEv, false);
            RaiseLocalEvent(user, userEv, false);
        }

        private void OnItemWielded(EntityUid uid, WieldableComponent component, ItemWieldedEvent args)
        {
            if (args.User == null)
                return;

            if (!CanWield(uid, component, args.User.Value) || component.Wielded)
                return;

            if (TryComp<SharedItemComponent>(uid, out var item))
            {
                component.OldInhandPrefix = item.EquippedPrefix;
                item.EquippedPrefix = component.WieldedInhandPrefix;
            }

            component.Wielded = true;

            if (component.WieldSound != null)
            {
                SoundSystem.Play(Filter.Pvs(uid), component.WieldSound.GetSound());
            }

            for (var i = 0; i < component.FreeHandsRequired; i++)
            {
                _virtualItemSystem.TrySpawnVirtualItemInHand(uid, args.User.Value);
            }

            args.User.Value.PopupMessage(Loc.GetString("wieldable-component-successful-wield",
                ("item", uid)));
        }

        private void OnItemUnwielded(EntityUid uid, WieldableComponent component, ItemUnwieldedEvent args)
        {
            if (args.User == null)
                return;
            if (!component.Wielded)
                return;

            if (TryComp<SharedItemComponent>(uid, out var item))
            {
                item.EquippedPrefix = component.OldInhandPrefix;
            }

            component.Wielded = false;

            if (!args.Force) // don't play sound/popup if this was a forced unwield
            {
                if (component.UnwieldSound != null)
                {
                    SoundSystem.Play(Filter.Pvs(uid),
                        component.UnwieldSound.GetSound());
                }

                args.User.Value.PopupMessage(Loc.GetString("wieldable-component-failed-wield",
                    ("item", uid)));
            }

            _virtualItemSystem.DeleteInHandsMatching(args.User.Value, uid);
        }

        private void OnItemLeaveHand(EntityUid uid, WieldableComponent component, UnequippedHandEvent args)
        {
            if (!component.Wielded || component.Owner != args.Unequipped)
                return;
            RaiseLocalEvent(uid, new ItemUnwieldedEvent(args.User, force: true));
        }

        private void OnVirtualItemDeleted(EntityUid uid, WieldableComponent component, VirtualItemDeletedEvent args)
        {
            if (args.BlockingEntity == uid && component.Wielded)
                AttemptUnwield(args.BlockingEntity, component, args.User);
        }

        private void OnMeleeHit(EntityUid uid, IncreaseDamageOnWieldComponent component, MeleeHitEvent args)
        {
            if (EntityManager.TryGetComponent<WieldableComponent>(uid, out var wield))
            {
                if (!wield.Wielded)
                    return;
            }
            if (args.Handled)
                return;

            args.ModifiersList.Add(component.Modifiers);
        }
    }

    #region Events

    public sealed class BeforeWieldEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    ///     Raised on the item that has been wielded.
    /// </summary>
    public sealed class ItemWieldedEvent : EntityEventArgs
    {
        public EntityUid? User;

        public ItemWieldedEvent(EntityUid? user = null)
        {
            User = user;
        }
    }

    /// <summary>
    ///     Raised on the user who wielded the item.
    /// </summary>
    public sealed class WieldedItemEvent : EntityEventArgs
    {
        public EntityUid Item;

        public WieldedItemEvent(EntityUid item)
        {
            Item = item;
        }
    }

    public sealed class BeforeUnwieldEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    ///     Raised on the item that has been unwielded.
    /// </summary>
    public sealed class ItemUnwieldedEvent : EntityEventArgs
    {
        public EntityUid? User;
        /// <summary>
        ///     Whether the item is being forced to be unwielded, or if the player chose to unwield it themselves.
        /// </summary>
        public bool Force;

        public ItemUnwieldedEvent(EntityUid? user = null, bool force=false)
        {
            User = user;
            Force = force;
        }
    }

    /// <summary>
    ///     Raised on the user who unwielded the item.
    /// </summary>
    public sealed class UnwieldedItemEvent : EntityEventArgs
    {
        public EntityUid Item;

        public UnwieldedItemEvent(EntityUid item)
        {
            Item = item;
        }
    }

    #endregion
}
