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
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<StunnableComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<StunnableComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<StunnableComponent, InteractHandEvent>(OnInteractHand);

            // Attempt event subscriptions.
            SubscribeLocalEvent<StunnableComponent, MovementAttemptEvent>(OnMoveAttempt);
            SubscribeLocalEvent<StunnableComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<StunnableComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<StunnableComponent, ThrowAttemptEvent>(OnThrowAttempt);
            SubscribeLocalEvent<StunnableComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<StunnableComponent, PickupAttemptEvent>(OnPickupAttempt);
            SubscribeLocalEvent<StunnableComponent, AttackAttemptEvent>(OnAttackAttempt);
            SubscribeLocalEvent<StunnableComponent, EquipAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<StunnableComponent, UnequipAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<StunnableComponent, StandAttemptEvent>(OnStandAttempt);
        }

        private void OnGetState(EntityUid uid, StunnableComponent stunnable, ref ComponentGetState args)
        {
            args.State = new StunnableComponentState(stunnable.StunnedTimer, stunnable.KnockdownTimer, stunnable.SlowdownTimer, stunnable.WalkSpeedMultiplier, stunnable.RunSpeedMultiplier);
        }

        private void OnHandleState(EntityUid uid, StunnableComponent stunnable, ref ComponentHandleState args)
        {
            if (args.Current is not StunnableComponentState state)
                return;

            stunnable.StunnedTimer = state.StunnedTimer;
            stunnable.KnockdownTimer = state.KnockdownTimer;
            stunnable.SlowdownTimer = state.SlowdownTimer;

            stunnable.WalkSpeedMultiplier = state.WalkSpeedMultiplier;
            stunnable.RunSpeedMultiplier = state.RunSpeedMultiplier;

            if (EntityManager.TryGetComponent(uid, out MovementSpeedModifierComponent? movement))
                movement.RefreshMovementSpeedModifiers();
        }

        private TimeSpan AdjustTime(TimeSpan time, (TimeSpan Start, TimeSpan End)? timer, float cap)
        {
            if (timer != null)
            {
                time = timer.Value.End - timer.Value.Start + time;
            }

            if (time.TotalSeconds > cap)
                time = TimeSpan.FromSeconds(cap);

            return time;
        }

        // TODO STUN: Make events for different things. (Getting modifiers, attempt events, informative events...)

        /// <summary>
        ///     Stuns the entity, disallowing it from doing many interactions temporarily.
        /// </summary>
        public void Stun(EntityUid uid, TimeSpan time,
            StunnableComponent? stunnable = null,
            SharedAlertsComponent? alerts = null)
        {
            if (!Resolve(uid, ref stunnable))
                return;

            time = AdjustTime(time, stunnable.StunnedTimer, stunnable.StunCap);

            if (time <= TimeSpan.Zero)
                return;

            stunnable.StunnedTimer = (_gameTiming.CurTime, _gameTiming.CurTime + time);

            SetAlert(uid, stunnable, alerts);

            stunnable.Dirty();
        }

        /// <summary>
        ///     Knocks down the entity, making it fall to the ground.
        /// </summary>
        public void Knockdown(EntityUid uid, TimeSpan time,
            StunnableComponent? stunnable = null,
            SharedAlertsComponent? alerts = null,
            StandingStateComponent? standingState = null,
            SharedAppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref stunnable))
                return;

            time = AdjustTime(time, stunnable.KnockdownTimer, stunnable.KnockdownCap);

            if (time <= TimeSpan.Zero)
                return;

            // Check if we can actually knock down the mob.
            if (!_standingStateSystem.Down(uid, standingState:standingState, appearance:appearance))
                return;

            stunnable.KnockdownTimer = (_gameTiming.CurTime, _gameTiming.CurTime + time);;

            SetAlert(uid, stunnable, alerts);

            stunnable.Dirty();
        }
        /// <summary>
        ///     Applies knockdown and stun to the entity temporarily.
        /// </summary>
        public void Paralyze(EntityUid uid, TimeSpan time,
            StunnableComponent? stunnable = null,
            SharedAlertsComponent? alerts = null)
        {
            if (!Resolve(uid, ref stunnable))
                return;

            // Optional component.
            Resolve(uid, ref alerts, false);

            Stun(uid, time, stunnable, alerts);
            Knockdown(uid, time, stunnable, alerts);
        }

        /// <summary>
        ///     Slows down the mob's walking/running speed temporarily
        /// </summary>
        public void Slowdown(EntityUid uid, TimeSpan time, float walkSpeedMultiplier = 1f, float runSpeedMultiplier = 1f,
            StunnableComponent? stunnable = null,
            MovementSpeedModifierComponent? speedModifier = null,
            SharedAlertsComponent? alerts = null)
        {
            if (!Resolve(uid, ref stunnable))
                return;

            // "Optional" component.
            Resolve(uid, ref speedModifier, false);

            time = AdjustTime(time, stunnable.SlowdownTimer, stunnable.SlowdownCap);

            if (time <= TimeSpan.Zero)
                return;

            // Doesn't make much sense to have the "Slowdown" method speed up entities now does it?
            walkSpeedMultiplier = Math.Clamp(walkSpeedMultiplier, 0f, 1f);
            runSpeedMultiplier = Math.Clamp(runSpeedMultiplier, 0f, 1f);

            stunnable.WalkSpeedMultiplier *= walkSpeedMultiplier;
            stunnable.RunSpeedMultiplier *= runSpeedMultiplier;

            stunnable.SlowdownTimer = (_gameTiming.CurTime, _gameTiming.CurTime + time);

            speedModifier?.RefreshMovementSpeedModifiers();

            SetAlert(uid, stunnable, alerts);
            stunnable.Dirty();
        }

        public void Reset(EntityUid uid,
            StunnableComponent? stunnable = null,
            MovementSpeedModifierComponent? speedModifier = null,
            StandingStateComponent? standingState = null,
            SharedAppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref stunnable))
                return;

            // Optional component.
            Resolve(uid, ref speedModifier, false);

            stunnable.StunnedTimer = null;
            stunnable.SlowdownTimer = null;
            stunnable.KnockdownTimer = null;

            speedModifier?.RefreshMovementSpeedModifiers();
            _standingStateSystem.Stand(uid, standingState, appearance);

            stunnable.Dirty();
        }

        private void SetAlert(EntityUid uid,
            StunnableComponent? stunnable = null,
            SharedAlertsComponent? alerts = null)
        {
            // This method is really just optional, doesn't matter if the entity doesn't support alerts.
            if (!Resolve(uid, ref stunnable, ref alerts, false))
                return;

            if (GetTimers(uid, stunnable) is not {} timers)
                return;

            alerts.ShowAlert(AlertType.Stun, cooldown:timers);
        }

        private (TimeSpan, TimeSpan)? GetTimers(EntityUid uid, StunnableComponent? stunnable = null)
        {
            if (!Resolve(uid, ref stunnable))
                return null;

            // Don't do anything if no stuns are applied.
            if (!stunnable.AnyStunActive)
                return null;

            TimeSpan start = TimeSpan.MaxValue, end = TimeSpan.MinValue;

            if (stunnable.StunnedTimer != null)
            {
                if (stunnable.StunnedTimer.Value.Start < start)
                    start = stunnable.StunnedTimer.Value.Start;

                if (stunnable.StunnedTimer.Value.End > end)
                    end = stunnable.StunnedTimer.Value.End;
            }

            if (stunnable.KnockdownTimer != null)
            {
                if (stunnable.KnockdownTimer.Value.Start < start)
                    start = stunnable.KnockdownTimer.Value.Start;

                if (stunnable.KnockdownTimer.Value.End > end)
                    end = stunnable.KnockdownTimer.Value.End;
            }

            if (stunnable.SlowdownTimer != null)
            {
                if (stunnable.SlowdownTimer.Value.Start < start)
                    start = stunnable.SlowdownTimer.Value.Start;

                if (stunnable.SlowdownTimer.Value.End > end)
                    end = stunnable.SlowdownTimer.Value.End;
            }

            return (start, end);
        }

        private void OnInteractHand(EntityUid uid, StunnableComponent stunnable, InteractHandEvent args)
        {
            if (args.Handled || stunnable.HelpTimer > 0f || !stunnable.KnockedDown)
                return;

            // Set it to half the help interval so helping is actually useful...
            stunnable.HelpTimer = stunnable.HelpInterval/2f;

            stunnable.KnockdownTimer = (stunnable.KnockdownTimer!.Value.Start, stunnable.KnockdownTimer.Value.End - TimeSpan.FromSeconds(stunnable.HelpInterval));

            SoundSystem.Play(Filter.Pvs(uid), stunnable.StunAttemptSound.GetSound(), uid, AudioHelpers.WithVariation(0.05f));

            SetAlert(uid, stunnable);
            stunnable.Dirty();

            args.Handled = true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var curTime = _gameTiming.CurTime;

            foreach (var stunnable in EntityManager.EntityQuery<StunnableComponent>())
            {
                var uid = stunnable.Owner.Uid;

                if(stunnable.HelpTimer > 0f)
                    // If it goes negative, that's okay.
                    stunnable.HelpTimer -= frameTime;

                if (stunnable.StunnedTimer != null)
                {
                    if (stunnable.StunnedTimer.Value.End <= curTime)
                    {
                        stunnable.StunnedTimer = null;
                        stunnable.Dirty();
                    }
                }

                if (stunnable.KnockdownTimer != null)
                {
                    if (stunnable.KnockdownTimer.Value.End <= curTime)
                    {
                        stunnable.KnockdownTimer = null;

                        // Try to stand up the mob...
                        _standingStateSystem.Stand(uid);

                        stunnable.Dirty();
                    }
                }

                if (stunnable.SlowdownTimer != null)
                {
                    if (stunnable.SlowdownTimer.Value.End <= curTime)
                    {
                        if (EntityManager.TryGetComponent(uid, out MovementSpeedModifierComponent? movement))
                            movement.RefreshMovementSpeedModifiers();


                        stunnable.SlowdownTimer = null;
                        stunnable.Dirty();
                    }
                }

                if (stunnable.AnyStunActive || !EntityManager.TryGetComponent(uid, out SharedAlertsComponent? status)
                                            || !status.IsShowingAlert(AlertType.Stun))
                    continue;

                status.ClearAlert(AlertType.Stun);
            }
        }

        #region Attempt Event Handling

        private void OnMoveAttempt(EntityUid uid, StunnableComponent stunnable, MovementAttemptEvent args)
        {
            if (stunnable.Stunned)
                args.Cancel();
        }

        private void OnInteractAttempt(EntityUid uid, StunnableComponent stunnable, InteractionAttemptEvent args)
        {
            if(stunnable.Stunned)
                args.Cancel();
        }

        private void OnUseAttempt(EntityUid uid, StunnableComponent stunnable, UseAttemptEvent args)
        {
            if(stunnable.Stunned)
                args.Cancel();
        }

        private void OnThrowAttempt(EntityUid uid, StunnableComponent stunnable, ThrowAttemptEvent args)
        {
            if (stunnable.Stunned)
                args.Cancel();
        }

        private void OnDropAttempt(EntityUid uid, StunnableComponent stunnable, DropAttemptEvent args)
        {
            if(stunnable.Stunned)
                args.Cancel();
        }

        private void OnPickupAttempt(EntityUid uid, StunnableComponent stunnable, PickupAttemptEvent args)
        {
            if(stunnable.Stunned)
                args.Cancel();
        }

        private void OnAttackAttempt(EntityUid uid, StunnableComponent stunnable, AttackAttemptEvent args)
        {
            if(stunnable.Stunned)
                args.Cancel();
        }

        private void OnEquipAttempt(EntityUid uid, StunnableComponent stunnable, EquipAttemptEvent args)
        {
            if(stunnable.Stunned)
                args.Cancel();
        }

        private void OnUnequipAttempt(EntityUid uid, StunnableComponent stunnable, UnequipAttemptEvent args)
        {
            if(stunnable.Stunned)
                args.Cancel();
        }

        private void OnStandAttempt(EntityUid uid, StunnableComponent stunnable, StandAttemptEvent args)
        {
            if(stunnable.KnockedDown)
                args.Cancel();
        }

        #endregion

    }
}
