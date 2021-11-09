using Content.Shared.DragDrop;
using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Body.Metabolism;
using Content.Shared.Movement;
using Content.Shared.Speech;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.ActionBlocker
{
    /// <summary>
    /// Utility methods to check if a specific entity is allowed to perform an action.
    /// </summary>
    [UsedImplicitly]
    public class ActionBlockerSystem : EntitySystem
    {
        // TODO: Make the EntityUid the main overload for all these methods.
        // TODO: Move each of these to their relevant EntitySystems?

        public bool CanMove(EntityUid uid)
        {
            var ev = new MovementAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanInteract(IEntity entity)
        {
            var ev = new InteractionAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            return !ev.Cancelled;
        }

        public bool CanInteract(EntityUid uid)
        {
            return CanInteract(EntityManager.GetEntity(uid));
        }

        public bool CanUse(EntityUid uid)
        {
            var ev = new UseAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanThrow(EntityUid uid)
        {
            var ev = new ThrowAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanSpeak(EntityUid uid)
        {
            var ev = new SpeakAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanDrop(EntityUid uid)
        {
            var ev = new DropAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanPickup(EntityUid uid)
        {
            var ev = new PickupAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanEmote(EntityUid uid)
        {
            var ev = new EmoteAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanAttack(EntityUid uid)
        {
            var ev = new AttackAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanEquip(EntityUid uid)
        {
            var ev = new EquipAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanUnequip(EntityUid uid)
        {
            var ev = new UnequipAttemptEvent(uid);
            RaiseLocalEvent(uid, ev);

            return !ev.Cancelled;
        }

        public bool CanChangeDirection(IEntity entity)
        {
            var ev = new ChangeDirectionAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            return !ev.Cancelled;
        }

        public bool CanChangeDirection(EntityUid uid)
        {
            return CanChangeDirection(EntityManager.GetEntity(uid));
        }

        public bool CanShiver(IEntity entity)
        {
            var ev = new ShiverAttemptEvent(entity);

            return !ev.Cancelled;
        }

        public bool CanShiver(EntityUid uid)
        {
            return CanShiver(EntityManager.GetEntity(uid));
        }

        public bool CanSweat(IEntity entity)
        {
            var ev = new SweatAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            return !ev.Cancelled;
        }

        public bool CanSweat(EntityUid uid)
        {
            return CanSweat(EntityManager.GetEntity(uid));
        }
    }
}
