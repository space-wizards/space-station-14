using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.EntitySystems;
using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.MobState.Components
{
    /// <summary>
    ///     When attached to an <see cref="DamageableComponent"/>,
    ///     this component will handle critical and death behaviors for mobs.
    ///     Additionally, it handles sending effects to clients
    ///     (such as blur effect for unconsciousness) and managing the health HUD.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    public class MobStateComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        /// <summary>
        ///     States that this <see cref="MobStateComponent"/> mapped to
        ///     the amount of damage at which they are triggered.
        ///     A threshold is reached when the total damage of an entity is equal
        ///     to or higher than the int key, but lower than the next threshold.
        ///     Ordered from lowest to highest.
        /// </summary>
        [ViewVariables]
        [DataField("thresholds")]
        private readonly SortedDictionary<int, IMobState> _lowestToHighestStates = new();

        // TODO Remove Nullability?
        [ViewVariables]
        public IMobState? CurrentState { get; private set; }

        [ViewVariables]
        public FixedPoint2? CurrentThreshold { get; private set; }

        public IEnumerable<KeyValuePair<int, IMobState>> _highestToLowestStates => _lowestToHighestStates.Reverse();

        protected override void Startup()
        {
            base.Startup();

            if (CurrentState != null && CurrentThreshold != null)
            {
                // Initialize with given states
                SetMobState(null, (CurrentState, CurrentThreshold.Value));
            }
            else
            {
                // Initialize with some amount of damage, defaulting to 0.
                UpdateState(_entMan.GetComponentOrNull<DamageableComponent>(Owner)?.TotalDamage ?? FixedPoint2.Zero);
            }
        }

        protected override void OnRemove()
        {
            EntitySystem.Get<AlertsSystem>().ClearAlert(Owner, AlertType.HumanHealth);

            base.OnRemove();
        }

        public override ComponentState GetComponentState()
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
                RemoveState();
            }
            else
            {
                UpdateState(state.CurrentThreshold.Value);
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

        public (IMobState state, FixedPoint2 threshold)? GetState(FixedPoint2 damage)
        {
            foreach (var (threshold, state) in _highestToLowestStates)
            {
                if (damage >= threshold)
                {
                    return (state, threshold);
                }
            }

            return null;
        }

        public bool TryGetState(
            FixedPoint2 damage,
            [NotNullWhen(true)] out IMobState? state,
            out FixedPoint2 threshold)
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

        private (IMobState state, FixedPoint2 threshold)? GetEarliestState(FixedPoint2 minimumDamage, Predicate<IMobState> predicate)
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

        private (IMobState state, FixedPoint2 threshold)? GetPreviousState(FixedPoint2 maximumDamage, Predicate<IMobState> predicate)
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

        public (IMobState state, FixedPoint2 threshold)? GetEarliestCriticalState(FixedPoint2 minimumDamage)
        {
            return GetEarliestState(minimumDamage, s => s.IsCritical());
        }

        public (IMobState state, FixedPoint2 threshold)? GetEarliestIncapacitatedState(FixedPoint2 minimumDamage)
        {
            return GetEarliestState(minimumDamage, s => s.IsIncapacitated());
        }

        public (IMobState state, FixedPoint2 threshold)? GetEarliestDeadState(FixedPoint2 minimumDamage)
        {
            return GetEarliestState(minimumDamage, s => s.IsDead());
        }

        public (IMobState state, FixedPoint2 threshold)? GetPreviousCriticalState(FixedPoint2 minimumDamage)
        {
            return GetPreviousState(minimumDamage, s => s.IsCritical());
        }

        private bool TryGetState(
            (IMobState state, FixedPoint2 threshold)? tuple,
            [NotNullWhen(true)] out IMobState? state,
            out FixedPoint2 threshold)
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
            FixedPoint2 minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out FixedPoint2 threshold)
        {
            var earliestState = GetEarliestCriticalState(minimumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        public bool TryGetEarliestIncapacitatedState(
            FixedPoint2 minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out FixedPoint2 threshold)
        {
            var earliestState = GetEarliestIncapacitatedState(minimumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        public bool TryGetEarliestDeadState(
            FixedPoint2 minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out FixedPoint2 threshold)
        {
            var earliestState = GetEarliestDeadState(minimumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        public bool TryGetPreviousCriticalState(
            FixedPoint2 maximumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out FixedPoint2 threshold)
        {
            var earliestState = GetPreviousCriticalState(maximumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        private void RemoveState()
        {
            var old = CurrentState;
            CurrentState = null;
            CurrentThreshold = null;

            SetMobState(old, null);
        }

        /// <summary>
        ///     Updates the mob state..
        /// </summary>
        public void UpdateState(FixedPoint2 damage)
        {
            if (!TryGetState(damage, out var newState, out var threshold))
            {
                return;
            }

            SetMobState(CurrentState, (newState, threshold));
        }

        /// <summary>
        ///     Sets the mob state and marks the component as dirty.
        /// </summary>
        private void SetMobState(IMobState? old, (IMobState state, FixedPoint2 threshold)? current)
        {
            var entMan = _entMan;

            if (!current.HasValue)
            {
                old?.ExitState(Owner, entMan);
                return;
            }

            var (state, threshold) = current.Value;

            CurrentThreshold = threshold;

            if (state == old)
            {
                state.UpdateState(Owner, threshold, entMan);
                return;
            }

            old?.ExitState(Owner, entMan);

            CurrentState = state;

            state.EnterState(Owner, entMan);
            state.UpdateState(Owner, threshold, entMan);

            var message = new MobStateChangedEvent(this, old, state);
            entMan.EventBus.RaiseLocalEvent(Owner, message);
            Dirty();
        }
    }

    [Serializable, NetSerializable]
    public class MobStateComponentState : ComponentState
    {
        public readonly FixedPoint2? CurrentThreshold;

        public MobStateComponentState(FixedPoint2? currentThreshold)
        {
            CurrentThreshold = currentThreshold;
        }
    }
}
