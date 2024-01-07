using Content.Shared.Body.Events;
using Content.Shared.DragDrop;
using Content.Shared.Emoting;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Speech;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.ActionBlocker
{
    /// <summary>
    /// Utility methods to check if a specific entity is allowed to perform an action.
    /// </summary>
    [UsedImplicitly]
    public sealed class ActionBlockerSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<InputMoverComponent, ComponentStartup>(OnMoverStartup);
        }

        private void OnMoverStartup(EntityUid uid, InputMoverComponent component, ComponentStartup args)
        {
            UpdateCanMove(uid, component);
        }

        public bool CanMove(EntityUid uid, InputMoverComponent? component = null)
        {
            return Resolve(uid, ref component, false) && component.CanMove;
        }

        public bool UpdateCanMove(EntityUid uid, InputMoverComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return false;

            var ev = new UpdateCanMoveEvent(uid);
            RaiseLocalEvent(uid, ev);

            if (component.CanMove == ev.Cancelled)
                Dirty(uid, component);

            component.CanMove = !ev.Cancelled;
            return !ev.Cancelled;
        }

        /// <summary>
        ///     Raises an event directed at both the user and the target entity to check whether a user is capable of
        ///     interacting with this entity.
        /// </summary>
        /// <remarks>
        ///     If this is a generic interaction without a target (e.g., stop-drop-and-roll when burning), the target
        ///     may be null. Note that this is checked by <see cref="SharedInteractionSystem"/>. In the majority of
        ///     cases, systems that provide interactions will not need to check this themselves, though they may need to
        ///     check other blockers like <see cref="CanPickup(EntityUid)"/>
        /// </remarks>
        /// <returns></returns>
        public bool CanInteract(EntityUid user, EntityUid? target)
        {
            var ev = new InteractionAttemptEvent(user, target);
            RaiseLocalEvent(user, ev);

            if (ev.Cancelled)
                return false;

            if (target == null || target == user)
                return true;

            var targetEv = new GettingInteractedWithAttemptEvent(user, target);
            RaiseLocalEvent(target.Value, targetEv);

            return !targetEv.Cancelled;
        }

        /// <summary>
        ///     Can a user utilize the entity that they are currently holding in their hands.
        /// </summary>>
        /// <remarks>
        ///     This event is automatically checked by <see cref="SharedInteractionSystem"/> for any interactions that
        ///     involve using a held entity. In the majority of cases, systems that provide interactions will not need
        ///     to check this themselves.
        /// </remarks>
        public bool CanUseHeldEntity(EntityUid user)
        {
            var ev = new UseAttemptEvent(user);
            RaiseLocalEvent(user, ev);

            return !ev.Cancelled;
        }

        public bool CanThrow(EntityUid user, EntityUid itemUid)
        {
            var ev = new ThrowAttemptEvent(user, itemUid);
            RaiseLocalEvent(user, ev);

            return !ev.Cancelled;
        }

        public bool CanSpeak(EntityUid uid)
        {
            // This one is used as broadcast
            var ev = new SpeakAttemptEvent(uid);
            RaiseLocalEvent(uid, ev, true);

            return !ev.Cancelled;
        }

        public bool CanDrop(EntityUid uid)
        {
            var ev = new DropAttemptEvent();
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanPickup(EntityUid user, EntityUid item)
        {
            var userEv = new PickupAttemptEvent(user, item);
            RaiseLocalEvent(user, userEv);

            if (userEv.Cancelled)
                return false;

            var itemEv = new GettingPickedUpAttemptEvent(user, item);
            RaiseLocalEvent(item, itemEv);

            return !itemEv.Cancelled;
        }

        public bool CanEmote(EntityUid uid)
        {
            // This one is used as broadcast
            var ev = new EmoteAttemptEvent(uid);
            RaiseLocalEvent(uid, ev, true);

            return !ev.Cancelled;
        }

        public bool CanAttack(EntityUid uid, EntityUid? target = null, Entity<MeleeWeaponComponent>? weapon = null, bool disarm = false)
        {
            _container.TryGetOuterContainer(uid, Transform(uid), out var outerContainer);
            if (target != null &&  target != outerContainer?.Owner && _container.IsEntityInContainer(uid))
            {
                var containerEv = new CanAttackFromContainerEvent(uid, target);
                RaiseLocalEvent(uid, containerEv);
                return containerEv.CanAttack;
            }

            var ev = new AttackAttemptEvent(uid, target, weapon, disarm);
            RaiseLocalEvent(uid, ev);

            if (ev.Cancelled)
                return false;

            if (target != null)
            {
                var tev = new GettingAttackedAttemptEvent();
                RaiseLocalEvent(target.Value, ref tev);
                return !tev.Cancelled;
            }

            return true;
        }

        public bool CanChangeDirection(EntityUid uid)
        {
            var ev = new ChangeDirectionAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanShiver(EntityUid uid)
        {
            var ev = new ShiverAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanSweat(EntityUid uid)
        {
            var ev = new SweatAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }
    }
}
