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

            SubscribeLocalEvent<MovementAttemptEvent>(OnMoveAttempt);
            SubscribeLocalEvent<InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<ThrowAttemptEvent>(OnThrwoAttempt);
            SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<PickupAttemptEvent>(OnPickupAttempt);
            SubscribeLocalEvent<EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<AttackAttemptEvent>(OnAttackAttempt);
            SubscribeLocalEvent<EquipAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<UnequipAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);
            SubscribeLocalEvent<ShiverAttemptEvent>(OnShiverAttempt);
            SubscribeLocalEvent<SweatAttemptEvent>(OnSweatAttempt);
        }

        private void OnMoveAttempt(MovementAttemptEvent ev)
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

        private void OnInteractAttempt(InteractionAttemptEvent ev)
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

        private void OnUseAttempt(UseAttemptEvent ev)
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

        private void OnThrwoAttempt(ThrowAttemptEvent ev)
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

        private void OnSpeakAttempt(SpeakAttemptEvent ev)
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

        private void OnDropAttempt(DropAttemptEvent ev)
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

        private void OnPickupAttempt(PickupAttemptEvent ev)
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

        private void OnEmoteAttempt(EmoteAttemptEvent ev)
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

        private void OnAttackAttempt(AttackAttemptEvent ev)
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

        private void OnEquipAttempt(EquipAttemptEvent ev)
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

        private void OnUnequipAttempt(UnequipAttemptEvent ev)
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

        private void OnChangeDirectionAttempt(ChangeDirectionAttemptEvent ev)
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

        private void OnShiverAttempt(ShiverAttemptEvent ev)
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

        private void OnSweatAttempt(SweatAttemptEvent ev)
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
