using Content.Shared.DragDrop;
using Content.Shared.EffectBlocker;
using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Metabolism.Events;
using Content.Shared.Movement;
using Content.Shared.Speech;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.ActionBlocker
{
    /// <summary>
    /// Utility methods to check if a specific entity is allowed to perform an action.
    /// For effects see <see cref="EffectBlockerSystem"/>
    /// </summary>
    [UsedImplicitly]
    public class ActionBlockerSystem : EntitySystem
    {
        public bool CanMove(IEntity entity)
        {
            var ev = new MovementAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanMove())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanInteract(IEntity entity)
        {
            var ev = new InteractionAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanInteract())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanUse(IEntity entity)
        {
            var ev = new UseAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanUse())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanThrow(IEntity entity)
        {
            var ev = new ThrowAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanThrow())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanSpeak(IEntity entity)
        {
            var ev = new SpeakAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanSpeak())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanDrop(IEntity entity)
        {
            var ev = new DropAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanDrop())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanPickup(IEntity entity)
        {
            var ev = new PickupAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanPickup())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanEmote(IEntity entity)
        {
            var ev = new EmoteAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanEmote())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanAttack(IEntity entity)
        {
            var ev = new AttackAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanAttack())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanEquip(IEntity entity)
        {
            var ev = new EquipAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanEquip())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanUnequip(IEntity entity)
        {
            var ev = new UnequipAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanUnequip())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanChangeDirection(IEntity entity)
        {
            var ev = new ChangeDirectionAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanChangeDirection())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanShiver(IEntity entity)
        {
            var ev = new ShiverAttemptEvent(entity);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanShiver())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }

        public bool CanSweat(IEntity entity)
        {
            var ev = new SweatAttemptEvent(entity);

            RaiseLocalEvent(entity.Uid, ev);

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanSweat())
                {
                    ev.Cancel();
                    break;
                }
            }

            return !ev.Cancelled;
        }
    }
}
