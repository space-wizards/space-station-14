using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Items;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Server.Wieldable
{
    public class WieldableSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WieldableComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<WieldableComponent, ItemWieldedEvent>(OnItemWielded);
            SubscribeLocalEvent<WieldableComponent, ItemUnwieldedEvent>(OnItemUnwielded);
            SubscribeLocalEvent<WieldableComponent, DroppedEvent>((uid, comp, _) => OnItemLeaveHand(uid, comp));
            SubscribeLocalEvent<WieldableComponent, ThrowEvent>((uid, comp, _) => OnItemLeaveHand(uid, comp));
        }

        private void OnUseInHand(EntityUid uid, WieldableComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;
            AttemptWield(uid, component, args.User);
        }

        /// <summary>
        ///     Attempts to wield an item, creating a doafter.
        /// </summary>
        public void AttemptWield(EntityUid uid, WieldableComponent component, IEntity user)
        {
            var ev = new BeforeWieldEvent();
            RaiseLocalEvent(uid, ev, false);
            var used = EntityManager.GetEntity(uid);

            if (ev.Cancelled) return;

            var doargs = new DoAfterEventArgs(
                user,
                component.WieldTime,
                default,
                used
            )
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                TargetFinishedEvent = new ItemWieldedEvent(),
                UserFinishedEvent = new WieldedItemEvent(used)
            };

            _doAfter.DoAfter(doargs);
        }

        /// <summary>
        ///     Attempts to unwield an item, creating a doafter.
        /// </summary>
        public void AttemptUnwield(EntityUid uid, WieldableComponent component, IEntity user)
        {
            var ev = new BeforeUnwieldEvent();
            RaiseLocalEvent(uid, ev, false);
            var used = EntityManager.GetEntity(uid);

            if (ev.Cancelled) return;

            var doargs = new DoAfterEventArgs(
                user,
                component.WieldTime,
                default,
                used
            )
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                TargetFinishedEvent = new ItemUnwieldedEvent(),
                UserFinishedEvent = new UnwieldedItemEvent(used)
            };

            _doAfter.DoAfter(doargs);
        }

        private void OnItemWielded(EntityUid uid, WieldableComponent component, ItemWieldedEvent args)
        {
            if (ComponentManager.TryGetComponent<ItemComponent>(uid, out var item))
            {
                component.OldInhandPrefix = item.EquippedPrefix;
                item.EquippedPrefix = component.WieldedInhandPrefix;
            }

            component.Wielded = true;

            if (component.WieldSound != null)
            {
                SoundSystem.Play(Filter.Pvs(EntityManager.GetEntity(uid)), component.WieldSound.GetSound());
            }


        }

        private void OnItemUnwielded(EntityUid uid, WieldableComponent component, ItemUnwieldedEvent args)
        {
            if (ComponentManager.TryGetComponent<ItemComponent>(uid, out var item))
            {
                item.EquippedPrefix = component.OldInhandPrefix;
            }

            component.Wielded = false;

            if (component.UnwieldSound != null && !args.Force) // don't play sound if this was a forced unwield
            {
                SoundSystem.Play(Filter.Pvs(EntityManager.GetEntity(uid)), component.UnwieldSound.GetSound());
            }
        }

        private void OnItemLeaveHand(EntityUid uid, WieldableComponent component)
        {
            RaiseLocalEvent(uid, new ItemUnwieldedEvent(true));
        }
    }

    #region Events

    public class BeforeWieldEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    ///     Raised on the item that has been wielded.
    /// </summary>
    public class ItemWieldedEvent : EntityEventArgs
    {
    }

    /// <summary>
    ///     Raised on the user who wielded the item.
    /// </summary>
    public class WieldedItemEvent : EntityEventArgs
    {
        public IEntity Item;

        public WieldedItemEvent(IEntity item)
        {
            Item = item;
        }
    }

    public class BeforeUnwieldEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    ///     Raised on the item that has been unwielded.
    /// </summary>
    public class ItemUnwieldedEvent : EntityEventArgs
    {
        /// <summary>
        ///     Whether the item is being forced to be unwielded, or if the player chose to unwield it themselves.
        /// </summary>
        public bool Force;

        public ItemUnwieldedEvent(bool force=false)
        {
            Force = force;
        }
    }

    /// <summary>
    ///     Raised on the user who unwielded the item.
    /// </summary>
    public class UnwieldedItemEvent : EntityEventArgs
    {
        public IEntity Item;

        public UnwieldedItemEvent(IEntity item)
        {
            Item = item;
        }
    }

    #endregion
}
