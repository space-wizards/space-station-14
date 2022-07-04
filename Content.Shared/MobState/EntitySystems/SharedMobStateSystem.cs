using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.Emoting;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;
using Content.Shared.Movement;
using Content.Shared.Movement.Events;
using Content.Shared.Pulling.Events;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Robust.Shared.Serialization;

namespace Content.Shared.MobState.EntitySystems
{
    public abstract partial class SharedMobStateSystem : EntitySystem
    {
        [Dependency] protected readonly AlertsSystem Alerts = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] protected readonly StandingStateSystem Standing = default!;
        [Dependency] protected readonly StatusEffectsSystem Status = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MobStateComponent, ComponentShutdown>(OnMobShutdown);
            SubscribeLocalEvent<MobStateComponent, ComponentStartup>(OnMobStartup);

            SubscribeLocalEvent<MobStateComponent, ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);
            SubscribeLocalEvent<MobStateComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<MobStateComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<MobStateComponent, ThrowAttemptEvent>(OnThrowAttempt);
            SubscribeLocalEvent<MobStateComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<MobStateComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<MobStateComponent, EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<MobStateComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<MobStateComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<MobStateComponent, PickupAttemptEvent>(OnPickupAttempt);
            SubscribeLocalEvent<MobStateComponent, StartPullAttemptEvent>(OnStartPullAttempt);
            SubscribeLocalEvent<MobStateComponent, DamageChangedEvent>(UpdateState);
            SubscribeLocalEvent<MobStateComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<MobStateComponent, StandAttemptEvent>(OnStandAttempt);
            SubscribeLocalEvent<MobStateChangedEvent>(OnStateChanged);
            // Note that there's no check for Down attempts because if a mob's in crit or dead, they can be downed...
        }

        private void OnMobStartup(EntityUid uid, MobStateComponent component, ComponentStartup args)
        {
            if (component.CurrentState != null && component.CurrentThreshold != null)
            {
                // Initialize with given states
                SetMobState(component, null, (component.CurrentState, component.CurrentThreshold.Value));
            }
            else
            {
                // Initialize with some amount of damage, defaulting to 0.
                UpdateState(component, CompOrNull<DamageableComponent>(uid)?.TotalDamage ?? FixedPoint2.Zero);
            }
        }

        private void OnMobShutdown(EntityUid uid, MobStateComponent component, ComponentShutdown args)
        {
            Alerts.ClearAlert(uid, AlertType.HumanHealth);
        }

        public bool IsAlive(EntityUid uid, MobStateComponent? component = null)
        {
            if (!Resolve(uid, ref component, false)) return false;
            return component.CurrentState == DamageState.Alive;
        }

        public bool IsCritical(EntityUid uid, MobStateComponent? component = null)
        {
            if (!Resolve(uid, ref component, false)) return false;
            return component.CurrentState == DamageState.Critical;
        }

        public bool IsDead(EntityUid uid, MobStateComponent? component = null)
        {
            if (!Resolve(uid, ref component, false)) return false;
            return component.CurrentState == DamageState.Dead;
        }

        public bool IsIncapacitated(EntityUid uid, MobStateComponent? component = null)
        {
            if (!Resolve(uid, ref component, false)) return false;
            return component.CurrentState == DamageState.Critical || component.CurrentState == DamageState.Dead;
        }

        #region ActionBlocker
        private void OnStateChanged(MobStateChangedEvent ev)
        {
            _blocker.UpdateCanMove(ev.Entity);
        }

        private void CheckAct(EntityUid uid, MobStateComponent component, CancellableEntityEventArgs args)
        {
            switch (component.CurrentState)
            {
                case SharedDeadMobState:
                case SharedCriticalMobState:
                    args.Cancel();
                    break;
            }
        }

        private void OnChangeDirectionAttempt(EntityUid uid, MobStateComponent component, ChangeDirectionAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnUseAttempt(EntityUid uid, MobStateComponent component, UseAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnInteractAttempt(EntityUid uid, MobStateComponent component, InteractionAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnThrowAttempt(EntityUid uid, MobStateComponent component, ThrowAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnSpeakAttempt(EntityUid uid, MobStateComponent component, SpeakAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnEquipAttempt(EntityUid uid, MobStateComponent component, IsEquippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Equipee == uid)
                CheckAct(uid, component, args);
        }

        private void OnEmoteAttempt(EntityUid uid, MobStateComponent component, EmoteAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnUnequipAttempt(EntityUid uid, MobStateComponent component, IsUnequippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Unequipee == uid)
                CheckAct(uid, component, args);
        }

        private void OnDropAttempt(EntityUid uid, MobStateComponent component, DropAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnPickupAttempt(EntityUid uid, MobStateComponent component, PickupAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        #endregion

        private void OnStartPullAttempt(EntityUid uid, MobStateComponent component, StartPullAttemptEvent args)
        {
            if (component.IsIncapacitated())
                args.Cancel();
        }

        public void UpdateState(EntityUid _, MobStateComponent component, DamageChangedEvent args)
        {
            UpdateState(component, args.Damageable.TotalDamage);
        }

        private void OnMoveAttempt(EntityUid uid, MobStateComponent component, UpdateCanMoveEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedCriticalMobState:
                case SharedDeadMobState:
                    args.Cancel();
                    return;
                default:
                    return;
            }
        }

        private void OnStandAttempt(EntityUid uid, MobStateComponent component, StandAttemptEvent args)
        {
            if (component.IsIncapacitated())
                args.Cancel();
        }

        public void RemoveState(MobStateComponent component)
        {
            var old = component.CurrentState;
            component.CurrentState = null;
            component.CurrentThreshold = null;

            SetMobState(component, old, null);
        }

        protected void EnterState(MobStateComponent component, DamageState? state)
        {
            switch (state)
            {
                case DamageState.Alive:
                    EnterNormState(component.Owner);
                    break;
                case DamageState.Critical:
                    EnterCritState(component.Owner);
                    break;
                case DamageState.Dead:
                    EnterDeadState(component.Owner);
                    break;
                case null:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected void UpdateState(MobStateComponent component, DamageState? state, FixedPoint2 threshold)
        {
            switch (state)
            {
                case DamageState.Alive:
                    UpdateNormState(component.Owner, threshold);
                    break;
                case DamageState.Critical:
                    UpdateCritState(component.Owner, threshold);
                    break;
                case DamageState.Dead:
                    UpdateDeadState(component.Owner, threshold);
                    break;
                case null:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected void ExitState(MobStateComponent component, DamageState? state)
        {
            switch (state)
            {
                case DamageState.Alive:
                    ExitNormState(component.Owner);
                    break;
                case DamageState.Critical:
                    ExitCritState(component.Owner);
                    break;
                case DamageState.Dead:
                    ExitDeadState(component.Owner);
                    break;
                case null:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        ///     Updates the mob state..
        /// </summary>
        public void UpdateState(MobStateComponent component, FixedPoint2 damage)
        {
            if (!TryGetState(component, damage, out var newState, out var threshold))
            {
                return;
            }

            SetMobState(component.CurrentState, (newState, threshold));
        }

        /// <summary>
        ///     Sets the mob state and marks the component as dirty.
        /// </summary>
        private void SetMobState(MobStateComponent component, DamageState? old, (DamageState state, FixedPoint2 threshold)? current)
        {
            if (!current.HasValue)
            {
                ExitState(component, old);
                return;
            }

            var (state, threshold) = current.Value;

            component.CurrentThreshold = threshold;

            if (state == old)
            {
                UpdateState(component, state, threshold);
                return;
            }

            ExitState(component, old);

            component.CurrentState = state;

            EnterState(component, state);
            UpdateState(component, state, threshold);

            var message = new MobStateChangedEvent(component, old, state);
            RaiseLocalEvent(component.Owner, message, true);
            Dirty(component);
        }

        public (DamageState state, FixedPoint2 threshold)? GetState(FixedPoint2 damage)
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
            [NotNullWhen(true)] out DamageState? state,
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

        private (DamageState state, FixedPoint2 threshold)? GetEarliestState(FixedPoint2 minimumDamage, Predicate<DamageState> predicate)
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

        private (DamageState state, FixedPoint2 threshold)? GetPreviousState(FixedPoint2 maximumDamage, Predicate<DamageState> predicate)
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

        public (DamageState state, FixedPoint2 threshold)? GetEarliestCriticalState(FixedPoint2 minimumDamage)
        {
            return GetEarliestState(minimumDamage, s => s.IsCritical());
        }

        public (DamageState state, FixedPoint2 threshold)? GetEarliestIncapacitatedState(FixedPoint2 minimumDamage)
        {
            return GetEarliestState(minimumDamage, s => s.IsIncapacitated());
        }

        public (DamageState state, FixedPoint2 threshold)? GetEarliestDeadState(FixedPoint2 minimumDamage)
        {
            return GetEarliestState(minimumDamage, s => s.IsDead());
        }

        public (DamageState state, FixedPoint2 threshold)? GetPreviousCriticalState(FixedPoint2 minimumDamage)
        {
            return GetPreviousState(minimumDamage, s => s.IsCritical());
        }

        private bool TryGetState(
            (DamageState state, FixedPoint2 threshold)? tuple,
            [NotNullWhen(true)] out DamageState? state,
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
            [NotNullWhen(true)] out DamageState? state,
            out FixedPoint2 threshold)
        {
            var earliestState = GetEarliestCriticalState(minimumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        public bool TryGetEarliestIncapacitatedState(
            FixedPoint2 minimumDamage,
            [NotNullWhen(true)] out DamageState? state,
            out FixedPoint2 threshold)
        {
            var earliestState = GetEarliestIncapacitatedState(minimumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        public bool TryGetEarliestDeadState(
            FixedPoint2 minimumDamage,
            [NotNullWhen(true)] out DamageState? state,
            out FixedPoint2 threshold)
        {
            var earliestState = GetEarliestDeadState(minimumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        public bool TryGetPreviousCriticalState(
            FixedPoint2 maximumDamage,
            [NotNullWhen(true)] out DamageState? state,
            out FixedPoint2 threshold)
        {
            var earliestState = GetPreviousCriticalState(maximumDamage);

            return TryGetState(earliestState, out state, out threshold);
        }

        [Serializable, NetSerializable]
        protected sealed class MobStateComponentState : ComponentState
        {
            public readonly FixedPoint2? CurrentThreshold;

            public MobStateComponentState(FixedPoint2? currentThreshold)
            {
                CurrentThreshold = currentThreshold;
            }
        }
    }
}
