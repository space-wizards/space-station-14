using System;
using Content.Shared.Alert;
using Content.Shared.Audio;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Stunnable
{
    [UsedImplicitly]
    public abstract class SharedStunSystem : EntitySystem
    {
        [Dependency] private readonly StandingStateSystem _standingStateSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
            SubscribeLocalEvent<KnockedDownComponent, ComponentRemove>(OnKnockRemove);

            SubscribeLocalEvent<SlowedDownComponent, ComponentInit>(OnSlowInit);
            SubscribeLocalEvent<SlowedDownComponent, ComponentRemove>(OnSlowRemove);

            SubscribeLocalEvent<SlowedDownComponent, ComponentGetState>(OnSlowGetState);
            SubscribeLocalEvent<SlowedDownComponent, ComponentHandleState>(OnSlowHandleState);

            SubscribeLocalEvent<KnockedDownComponent, ComponentGetState>(OnKnockGetState);
            SubscribeLocalEvent<KnockedDownComponent, ComponentHandleState>(OnKnockHandleState);

            // helping people up if they're knocked down
            SubscribeLocalEvent<KnockedDownComponent, InteractHandEvent>(OnInteractHand);

            // Attempt event subscriptions.
            SubscribeLocalEvent<StunnedComponent, MovementAttemptEvent>(OnMoveAttempt);
            SubscribeLocalEvent<StunnedComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<StunnedComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<StunnedComponent, ThrowAttemptEvent>(OnThrowAttempt);
            SubscribeLocalEvent<StunnedComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<StunnedComponent, PickupAttemptEvent>(OnPickupAttempt);
            SubscribeLocalEvent<StunnedComponent, AttackAttemptEvent>(OnAttackAttempt);
            SubscribeLocalEvent<StunnedComponent, EquipAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<StunnedComponent, UnequipAttemptEvent>(OnUnequipAttempt);
        }

        private void OnSlowGetState(EntityUid uid, SlowedDownComponent component, ref ComponentGetState args)
        {
            args.State = new SlowedDownComponentState(component.SprintSpeedModifier, component.WalkSpeedModifier);
        }

        private void OnSlowHandleState(EntityUid uid, SlowedDownComponent component, ref ComponentHandleState args)
        {
            if (args.Current is SlowedDownComponentState state)
            {
                component.SprintSpeedModifier = state.SprintSpeedModifier;
                component.WalkSpeedModifier = state.WalkSpeedModifier;
            }
        }

        private void OnKnockGetState(EntityUid uid, KnockedDownComponent component, ref ComponentGetState args)
        {
            args.State = new KnockedDownComponentState(component.HelpInterval, component.HelpTimer);
        }

        private void OnKnockHandleState(EntityUid uid, KnockedDownComponent component, ref ComponentHandleState args)
        {
            if (args.Current is KnockedDownComponentState state)
            {
                component.HelpInterval = state.HelpInterval;
                component.HelpTimer = state.HelpTimer;
            }
        }

        private void OnKnockInit(EntityUid uid, KnockedDownComponent component, ComponentInit args)
        {
            _standingStateSystem.Down(uid);
        }

        private void OnKnockRemove(EntityUid uid, KnockedDownComponent component, ComponentRemove args)
        {
            _standingStateSystem.Stand(uid);
        }

        private void OnSlowInit(EntityUid uid, SlowedDownComponent component, ComponentInit args)
        {
            // needs to be done so the client can also refresh when the addition is replicated,
            // if the initial status effect addition wasn't predicted
            if (EntityManager.TryGetComponent<MovementSpeedModifierComponent>(uid, out var move))
            {
                move.RefreshMovementSpeedModifiers();
            }
        }

        private void OnSlowRemove(EntityUid uid, SlowedDownComponent component, ComponentRemove args)
        {
            if (EntityManager.TryGetComponent<MovementSpeedModifierComponent>(uid, out var move))
            {
                component.SprintSpeedModifier = 1.0f;
                component.WalkSpeedModifier = 1.0f;
                move.RefreshMovementSpeedModifiers();
            }
        }

        // TODO STUN: Make events for different things. (Getting modifiers, attempt events, informative events...)

        /// <summary>
        ///     Stuns the entity, disallowing it from doing many interactions temporarily.
        /// </summary>
        public bool TryStun(EntityUid uid, TimeSpan time,
            StatusEffectsComponent? status = null,
            SharedAlertsComponent? alerts = null)
        {
            if (time <= TimeSpan.Zero)
                return false;

            Resolve(uid, ref alerts, false);

            return _statusEffectSystem.TryAddStatusEffect<StunnedComponent>(uid, "Stun", time, alerts: alerts);
        }

        /// <summary>
        ///     Knocks down the entity, making it fall to the ground.
        /// </summary>
        public bool TryKnockdown(EntityUid uid, TimeSpan time,
            StatusEffectsComponent? status = null,
            SharedAlertsComponent? alerts = null)
        {
            if (time <= TimeSpan.Zero)
                return false;

            Resolve(uid, ref alerts, false);

            return _statusEffectSystem.TryAddStatusEffect<KnockedDownComponent>(uid, "KnockedDown", time, alerts: alerts);
        }

        /// <summary>
        ///     Applies knockdown and stun to the entity temporarily.
        /// </summary>
        public bool TryParalyze(EntityUid uid, TimeSpan time,
            StatusEffectsComponent? status = null,
            SharedAlertsComponent? alerts = null)
        {
            // Optional component.
            Resolve(uid, ref alerts, false);

            return TryKnockdown(uid, time, status, alerts) && TryStun(uid, time, status, alerts);
        }

        /// <summary>
        ///     Slows down the mob's walking/running speed temporarily
        /// </summary>
        public bool TrySlowdown(EntityUid uid, TimeSpan time,
            float walkSpeedMultiplier = 1f, float runSpeedMultiplier = 1f,
            StatusEffectsComponent? status = null,
            MovementSpeedModifierComponent? speedModifier = null,
            SharedAlertsComponent? alerts = null)
        {
            // "Optional" component.
            Resolve(uid, ref speedModifier, false);

            if (time <= TimeSpan.Zero)
                return false;

            if (_statusEffectSystem.TryAddStatusEffect<SlowedDownComponent>(uid, "SlowedDown", time, status, alerts))
            {
                var slowed = EntityManager.GetComponent<SlowedDownComponent>(uid);
                // Doesn't make much sense to have the "TrySlowdown" method speed up entities now does it?
                walkSpeedMultiplier = Math.Clamp(walkSpeedMultiplier, 0f, 1f);
                runSpeedMultiplier = Math.Clamp(runSpeedMultiplier, 0f, 1f);

                slowed.WalkSpeedModifier *= walkSpeedMultiplier;
                slowed.SprintSpeedModifier *= runSpeedMultiplier;

                speedModifier?.RefreshMovementSpeedModifiers();

                return true;
            }

            return false;
        }

        private void OnInteractHand(EntityUid uid, KnockedDownComponent knocked, InteractHandEvent args)
        {
            if (args.Handled || knocked.HelpTimer > 0f)
                return;

            // Set it to half the help interval so helping is actually useful...
            knocked.HelpTimer = knocked.HelpInterval/2f;

            _statusEffectSystem.TryRemoveTime(uid, "KnockedDown", TimeSpan.FromSeconds(knocked.HelpInterval));

            SoundSystem.Play(Filter.Pvs(uid), knocked.StunAttemptSound.GetSound(), uid, AudioHelpers.WithVariation(0.05f));

            knocked.Dirty();

            args.Handled = true;
        }

        #region Attempt Event Handling

        private void OnMoveAttempt(EntityUid uid, StunnedComponent stunned, MovementAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnInteractAttempt(EntityUid uid, StunnedComponent stunned, InteractionAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnUseAttempt(EntityUid uid, StunnedComponent stunned, UseAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnThrowAttempt(EntityUid uid, StunnedComponent stunned, ThrowAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnDropAttempt(EntityUid uid, StunnedComponent stunned, DropAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnPickupAttempt(EntityUid uid, StunnedComponent stunned, PickupAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnAttackAttempt(EntityUid uid, StunnedComponent stunned, AttackAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnEquipAttempt(EntityUid uid, StunnedComponent stunned, EquipAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnUnequipAttempt(EntityUid uid, StunnedComponent stunned, UnequipAttemptEvent args)
        {
            args.Cancel();
        }

        #endregion

    }
}
