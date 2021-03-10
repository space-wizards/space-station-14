#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    /// <summary>
    ///     When attached to an <see cref="IDamageableComponent"/>,
    ///     this component will handle critical and death behaviors for mobs.
    ///     Additionally, it handles sending effects to clients
    ///     (such as blur effect for unconsciousness) and managing the health HUD.
    /// </summary>
    public abstract class SharedMobStateComponent : Component, IMobStateComponent, IActionBlocker
    {
        public override string Name => "MobState";

        public override uint? NetID => ContentNetIDs.MOB_STATE;

        /// <summary>
        ///     States that this <see cref="SharedMobStateComponent"/> mapped to
        ///     the amount of damage at which they are triggered.
        ///     A threshold is reached when the total damage of an entity is equal
        ///     to or higher than the int key, but lower than the next threshold.
        ///     Ordered from lowest to highest.
        /// </summary>
        [ViewVariables]
        [DataField("thresholds")]
        private readonly SortedDictionary<int, IMobState> _lowestToHighestStates = default!;

        // TODO Remove Nullability?
        [ViewVariables]
        public IMobState? CurrentState { get; private set; }

        [ViewVariables]
        public int? CurrentThreshold { get; private set; }

        public IEnumerable<KeyValuePair<int, IMobState>> _highestToLowestStates => _lowestToHighestStates.Reverse();

        protected override void Startup()
        {
            base.Startup();

            if (CurrentState != null && CurrentThreshold != null)
            {
                UpdateState(null, (CurrentState, CurrentThreshold.Value));
            }
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out SharedAlertsComponent? status))
            {
                status.ClearAlert(AlertType.HumanHealth);
            }

            base.OnRemove();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new MobStateComponentState(CurrentThreshold);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MobStateComponentState state)
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

        public (IMobState state, int threshold)? GetState(int damage)
        {
            foreach (var (threshold, state) in _lowestToHighestStates.Reverse())
            {
                if (damage >= threshold)
                {
                    return (state, threshold);
                }
            }

            return null;
        }

        public bool TryGetState(
            int damage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold)
        {
            var highestState = GetState(damage);

            if (highestState == null)
            {
                state = default;
                threshold = default;
                return false;
            }

            (state, threshold) = highestState.Value;
            return true;
        }

        private (IMobState state, int threshold)? GetEarliestState(int minimumDamage, Predicate<IMobState> predicate)
        {
            foreach (var (threshold, state) in _lowestToHighestStates)
            {
                if (threshold < minimumDamage ||
                    !predicate(state))
                {
                    continue;
                }

                return (state, threshold);
            }

            return null;
        }

        private (IMobState state, int threshold)? GetPreviousState(int maximumDamage, Predicate<IMobState> predicate)
        {
            foreach (var (threshold, state) in _highestToLowestStates)
            {
                if (threshold > maximumDamage ||
                    !predicate(state))
                {
                    continue;
                }

                return (state, threshold);
            }

            return null;
        }

        public (IMobState state, int threshold)? GetEarliestCriticalState(int minimumDamage)
        {
            return GetEarliestState(minimumDamage, s => s.IsCritical());
        }

        public (IMobState state, int threshold)? GetEarliestIncapacitatedState(int minimumDamage)
        {
            return GetEarliestState(minimumDamage, s => s.IsIncapacitated());
        }

        public (IMobState state, int threshold)? GetEarliestDeadState(int minimumDamage)
        {
            return GetEarliestState(minimumDamage, s => s.IsDead());
        }

        public (IMobState state, int threshold)? GetPreviousCriticalState(int minimumDamage)
        {
            return GetPreviousState(minimumDamage, s => s.IsCritical());
        }

        private bool TryGetState(
            (IMobState state, int threshold)? tuple,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold)
        {
            if (tuple == null)
            {
                state = default;
                threshold = default;
                return false;
            }

            (state, threshold) = tuple.Value;
            return true;
        }

        public bool TryGetEarliestCriticalState(
            int minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold)
        {
            var earliestState = GetEarliestCriticalState(minimumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        public bool TryGetEarliestIncapacitatedState(
            int minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold)
        {
            var earliestState = GetEarliestIncapacitatedState(minimumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        public bool TryGetEarliestDeadState(
            int minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold)
        {
            var earliestState = GetEarliestDeadState(minimumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        public bool TryGetPreviousCriticalState(
            int maximumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold)
        {
            var earliestState = GetPreviousCriticalState(maximumDamage);

            return TryGetState(earliestState, out state, out threshold);
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
            if (!TryGetState(damage, out var newState, out var threshold))
            {
                return;
            }

            UpdateState(CurrentState, (newState, threshold));

            if (!syncing)
            {
                Dirty();
            }
        }

        private void UpdateState(IMobState? old, (IMobState state, int threshold)? current)
        {
            if (!current.HasValue)
            {
                old?.ExitState(Owner);
                return;
            }

            var (state, threshold) = current.Value;

            CurrentThreshold = threshold;

            if (state == old)
            {
                state.UpdateState(Owner, threshold);
                return;
            }

            old?.ExitState(Owner);

            CurrentState = state;

            state.EnterState(Owner);
            state.UpdateState(Owner, threshold);

            var message = new MobStateChangedMessage(this, old, state);

            SendMessage(message);
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
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
