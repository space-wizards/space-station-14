using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Disease.Events;
using Content.Shared.DragDrop;
using Content.Shared.Emoting;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pulling.Events;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Strip.Components;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.MobState.EntitySystems
{
    public abstract partial class SharedMobStateSystem : EntitySystem
    {
        [Dependency] protected readonly AlertsSystem Alerts = default!;
        [Dependency] private   readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private   readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private   readonly SharedPhysicsSystem _physics = default!;
        [Dependency] protected readonly StatusEffectsSystem Status = default!;
        [Dependency] private   readonly StandingStateSystem _standing = default!;
        [Dependency] private   readonly ISharedAdminLogManager _adminLogger = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MobStateComponent, ComponentShutdown>(OnMobStateShutdown);
            SubscribeLocalEvent<MobStateComponent, ComponentStartup>(OnMobStateStartup);

            SubscribeLocalEvent<MobStateComponent, BeforeGettingStrippedEvent>(OnGettingStripped);

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
            SubscribeLocalEvent<MobStateComponent, DamageChangedEvent>(OnDamageRecieved);
            SubscribeLocalEvent<MobStateComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<MobStateComponent, StandAttemptEvent>(OnStandAttempt);
            SubscribeLocalEvent<MobStateComponent, TryingToSleepEvent>(OnSleepAttempt);
            SubscribeLocalEvent<MobStateComponent, AttemptSneezeCoughEvent>(OnSneezeAttempt);
            SubscribeLocalEvent<MobStateChangedEvent>(OnStateChanged);
            // Note that there's no check for Down attempts because if a mob's in crit or dead, they can be downed...
        }

        protected void UpdateMobState_Internal(EntityUid origin,MobStateComponent component)
        {
            //interate through the tickets in reverse order, set the highest state if tickets are present
            for (var i = component.StateTickets.Length; i >= 0; i--)
            {
                if (component.StateTickets[i] <= 0)
                    continue;
                SetMobState(origin,component, (MobState) (i + 1));
                return;
            }
        }

        public void UpdateMobState(EntityUid origin, MobStateComponent? component)
        {
            if (!Resolve(origin, ref component))
                return;
            UpdateMobState_Internal(origin, component);
        }

        private void SetMobState(EntityUid origin, MobStateComponent component, MobState newState)
        {
            if (component.CurrentState == newState)
                return;
            var oldState = component.CurrentState;
            component.CurrentState = newState;
            _adminLogger.Add(LogType.Damaged, oldState == MobState.Alive ? LogImpact.Low : LogImpact.Medium,
                $"{ToPrettyString(component.Owner):user} state changed from {oldState} to {newState}");
            var message = new MobStateChangedEvent(component, oldState, newState, origin);
            RaiseLocalEvent(component.Owner, message, true);
            Dirty(component);
        }

        private void OnMobStateStartup(EntityUid uid, MobStateComponent component, ComponentStartup args)
        {
            if (component.CurrentThreshold == null)
            {
                // Initialize with some amount of damage, defaulting to 0.
                CheckDamageThreshold(component, CompOrNull<DamageableComponent>(uid)?.TotalDamage ?? FixedPoint2.Zero);

            }
            else
            {
                // Initialize with given states
                //SetMobState(component, MobState.Invalid, (component.CurrentState, component.CurrentThreshold.Value));
            }
        }

        private void OnMobStateShutdown(EntityUid uid, MobStateComponent component, ComponentShutdown args)
        {
            Alerts.ClearAlert(uid, AlertType.HumanHealth);
        }

        public bool IsAlive(EntityUid uid, MobStateComponent? component = null)
        {
            if (!Resolve(uid, ref component, false)) return false;
            return component.CurrentState == MobState.Alive;
        }

        public bool IsCritical(EntityUid uid, MobStateComponent? component = null)
        {
            if (!Resolve(uid, ref component, false)) return false;
            return component.CurrentState == MobState.Critical;
        }

        public bool IsDead(EntityUid uid, MobStateComponent? component = null)
        {
            if (!Resolve(uid, ref component, false)) return false;
            return component.CurrentState == MobState.Dead;
        }

        public bool IsIncapacitated(EntityUid uid, MobStateComponent? component = null)
        {
            if (!Resolve(uid, ref component, false)) return false;
            return component.CurrentState is MobState.Critical or MobState.Dead;
        }

        public virtual void RemoveState(MobStateComponent component)
        {
            var old = component.CurrentState;
            component.CurrentState = MobState.Invalid;
            component.CurrentThreshold = null;

            SetMobState(component, old, null);
        }

        public virtual void EnterState(MobStateComponent? component, MobState? state)
        {
            // TODO: Thanks buckle
            if (component == null) return;

            switch (state)
            {
                case MobState.Alive:
                    EnterNormState(component.Owner);
                    break;
                case MobState.Critical:
                    EnterCritState(component.Owner);
                    break;
                case MobState.Dead:
                    EnterDeadState(component.Owner);
                    break;
                case null:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual void UpdateState(MobStateComponent component, MobState? state, FixedPoint2 threshold)
        {
            switch (state)
            {
                case MobState.Alive:
                    UpdateNormState(component.Owner, threshold);
                    break;
                case MobState.Critical:
                    UpdateCritState(component.Owner, threshold);
                    break;
                case MobState.Dead:
                    UpdateDeadState(component.Owner, threshold);
                    break;
                case null:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual void ExitState(MobStateComponent component, MobState? state)
        {
            switch (state)
            {
                case MobState.Alive:
                    ExitNormState(component.Owner);
                    break;
                case MobState.Critical:
                    ExitCritState(component.Owner);
                    break;
                case MobState.Dead:
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
        public void CheckDamageThreshold(MobStateComponent component, FixedPoint2 damage, EntityUid? origin = null)
        {
            if (!TryGetStateThreshold(component, damage, out var newState, out var threshold))
            {
                return;
            }

            SetMobState(component, component.CurrentState, (newState, threshold), origin);
        }

        public (MobState state, FixedPoint2 threshold)? GetState(MobStateComponent component, FixedPoint2 damage)
        {
            foreach (var (threshold, state) in component._highestToLowestStates)
            {
                if (damage >= threshold)
                {
                    return (state, threshold);
                }
            }

            return null;
        }


    }
}
