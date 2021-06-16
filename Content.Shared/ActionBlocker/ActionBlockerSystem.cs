#nullable enable
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
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MovementAttemptEvent>(CanMove);
            SubscribeLocalEvent<InteractionAttemptEvent>(CanInteract);
            SubscribeLocalEvent<UseAttemptEvent>(CanUse);
            SubscribeLocalEvent<ThrowAttemptEvent>(CanThrow);
            SubscribeLocalEvent<SpeakAttemptEvent>(CanSpeak);
            SubscribeLocalEvent<DropAttemptEvent>(CanDrop);
            SubscribeLocalEvent<PickupAttemptEvent>(CanPickup);
            SubscribeLocalEvent<EmoteAttemptEvent>(CanEmote);
            SubscribeLocalEvent<AttackAttemptEvent>(CanAttack);
            SubscribeLocalEvent<EquipAttemptEvent>(CanEquip);
            SubscribeLocalEvent<UnequipAttemptEvent>(CanUnequip);
            SubscribeLocalEvent<ChangeDirectionAttemptEvent>(CanChangeDirection);
            SubscribeLocalEvent<ShiverAttemptEvent>(CanShiver);
            SubscribeLocalEvent<SweatAttemptEvent>(CanSweat);
        }

        private void CanMove(MovementAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanMove())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanInteract(InteractionAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanInteract())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanUse(UseAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanUse())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanThrow(ThrowAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanThrow())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanSpeak(SpeakAttemptEvent ev)
        {
            if (!ev.Entity.HasComponent<SharedSpeechComponent>())
            {
                ev.Cancel();
                return;
            }

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanSpeak())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanDrop(DropAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanDrop())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanPickup(PickupAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanPickup())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanEmote(EmoteAttemptEvent ev)
        {
            if (!ev.Entity.HasComponent<SharedEmotingComponent>())
            {
                ev.Cancel();
                return;
            }

            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanEmote())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanAttack(AttackAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanAttack())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanEquip(EquipAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanEquip())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanUnequip(UnequipAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanUnequip())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanChangeDirection(ChangeDirectionAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanChangeDirection())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanShiver(ShiverAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanShiver())
                {
                    ev.Cancel();
                    break;
                }
            }
        }

        private void CanSweat(SweatAttemptEvent ev)
        {
            foreach (var blocker in ev.Entity.GetAllComponents<IActionBlocker>())
            {
                if (!blocker.CanSweat())
                {
                    ev.Cancel();
                    break;
                }
            }
        }
    }
}
