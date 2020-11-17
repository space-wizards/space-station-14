#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    /// <summary>
    ///     When attacked to an <see cref="IDamageableComponent"/>,
    ///     this component will handle critical and death behaviors for mobs.
    ///     Additionally, it handles sending effects to clients
    ///     (such as blur effect for unconsciousness) and managing the health HUD.
    /// </summary>
    public abstract class SharedMobStateComponent : Component, IMobStateComponent, IActionBlocker
    {
        public override string Name => "MobState";

        public override uint? NetID => ContentNetIDs.MOB_STATE;

        /// <summary>
        ///     Thresholds for this <see cref="SharedMobStateComponent"/>,
        ///     mapped from threshold to its corresponding state.
        ///     Ordered from lowest to highest.
        /// </summary>
        [ViewVariables]
        private SortedDictionary<int, IMobState> _lowestToHighestThresholds = default!;

        // TODO Remove Nullability?
        [ViewVariables]
        public IMobState? CurrentState { get; private set; }

        [ViewVariables]
        public int? CurrentThreshold { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "thresholds",
                new Dictionary<int, IMobState>(),
                thresholds =>
                {
                    _lowestToHighestThresholds = new SortedDictionary<int, IMobState>(thresholds);
                },
                () => new Dictionary<int, IMobState>(_lowestToHighestThresholds));
        }

        protected override void Startup()
        {
            base.Startup();

            if (CurrentState != null && CurrentThreshold != null)
            {
                UpdateState(null, (CurrentState, CurrentThreshold.Value));
            }
        }

        public override ComponentState GetComponentState()
        {
            return new MobStateComponentState(CurrentThreshold);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is MobStateComponentState state))
            {
                return;
            }

            if (CurrentThreshold == state.CurrentThreshold)
            {
                return;
            }

            if (state.CurrentThreshold == null)
            {
                RemoveState(true);
            }
            else
            {
                UpdateState(state.CurrentThreshold.Value, true);
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case DamageChangedMessage msg:
                    if (msg.Damageable.Owner != Owner)
                    {
                        break;
                    }

                    UpdateState(msg.Damageable.TotalDamage);

                    break;
            }
        }

        public bool IsAlive()
        {
            return CurrentState?.IsAlive() ?? false;
        }

        public bool IsCritical()
        {
            return CurrentState?.IsCritical() ?? false;
        }

        public bool IsDead()
        {
            return CurrentState?.IsDead() ?? false;
        }

        public bool IsIncapacitated()
        {
            return CurrentState?.IsIncapacitated() ?? false;
        }

        public (IMobState state, int threshold)? GetHighestThreshold(int damage)
        {
            foreach (var (threshold, state) in _lowestToHighestThresholds.Reverse())
            {
                if (damage >= threshold)
                {
                    return (state, threshold);
                }
            }

            return null;
        }

        public bool TryGetHighestThreshold(
            int damage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold)
        {
            var highestThreshold = GetHighestThreshold(damage);

            if (highestThreshold == null)
            {
                state = default;
                threshold = default;
                return false;
            }

            (state, threshold) = highestThreshold.Value;
            return true;
        }

        public (IMobState state, int threshold)? GetEarliestIncapacitatedThreshold(int minimumDamage)
        {
            foreach (var (threshold, state) in _lowestToHighestThresholds)
            {
                if (!state.IsIncapacitated())
                {
                    continue;
                }

                if (threshold < minimumDamage)
                {
                    continue;
                }

                return (state, threshold);
            }

            return null;
        }

        public bool TryGetEarliestIncapacitatedThreshold(
            int minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold)
        {
            var earliestThreshold = GetEarliestIncapacitatedThreshold(minimumDamage);

            if (earliestThreshold == null)
            {
                state = default;
                threshold = default;
                return false;
            }

            (state, threshold) = earliestThreshold.Value;
            return true;
        }

        private void RemoveState(bool syncing = false)
        {
            var old = CurrentState;
            CurrentState = null;
            CurrentThreshold = null;

            UpdateState(old, null);

            if (!syncing)
            {
                Dirty();
            }
        }

        public void UpdateState(int damage, bool syncing = false)
        {
            if (!TryGetHighestThreshold(damage, out var state, out var threshold))
            {
                return;
            }

            var old = CurrentState;
            CurrentState = state;
            CurrentThreshold = damage;

            UpdateState(old, (state, threshold));

            if (!syncing)
            {
                Dirty();
            }
        }

        private void UpdateState(IMobState? old, (IMobState state, int threshold)? current)
        {
            old?.ExitState(Owner);

            if (current.HasValue)
            {
                var (state, threshold) = current.Value;

                state.EnterState(Owner);
                state.UpdateState(Owner, threshold);

                var message = new MobStateChangedMessage(this, old, state);

                SendMessage(message);
            }
        }

        bool IActionBlocker.CanInteract()
        {
            return CurrentState?.CanInteract() ?? true;
        }

        bool IActionBlocker.CanMove()
        {
            return CurrentState?.CanMove() ?? true;
        }

        bool IActionBlocker.CanUse()
        {
            return CurrentState?.CanUse() ?? true;
        }

        bool IActionBlocker.CanThrow()
        {
            return CurrentState?.CanThrow() ?? true;
        }

        bool IActionBlocker.CanSpeak()
        {
            return CurrentState?.CanSpeak() ?? true;
        }

        bool IActionBlocker.CanDrop()
        {
            return CurrentState?.CanDrop() ?? true;
        }

        bool IActionBlocker.CanPickup()
        {
            return CurrentState?.CanPickup() ?? true;
        }

        bool IActionBlocker.CanEmote()
        {
            return CurrentState?.CanEmote() ?? true;
        }

        bool IActionBlocker.CanAttack()
        {
            return CurrentState?.CanAttack() ?? true;
        }

        bool IActionBlocker.CanEquip()
        {
            return CurrentState?.CanEquip() ?? true;
        }

        bool IActionBlocker.CanUnequip()
        {
            return CurrentState?.CanUnequip() ?? true;
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return CurrentState?.CanChangeDirection() ?? true;
        }
    }

    [Serializable, NetSerializable]
    public class MobStateComponentState : ComponentState
    {
        public readonly int? CurrentThreshold;

        public MobStateComponentState(int? currentThreshold) : base(ContentNetIDs.MOB_STATE)
        {
            CurrentThreshold = currentThreshold;
        }
    }
}
