#nullable enable
using System;
using System.Threading;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Stunnable
{
    [NetworkedComponent()]
    public abstract class SharedStunnableComponent : Component, IMoveSpeedModifier, IActionBlocker, IInteractHand
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public sealed override string Name => "Stunnable";

        public (TimeSpan Start, TimeSpan End)? StunnedTimer { get; protected set; }
        public (TimeSpan Start, TimeSpan End)? KnockdownTimer { get; protected set; }
        public (TimeSpan Start, TimeSpan End)? SlowdownTimer { get; protected set; }

        [ViewVariables] public float StunnedSeconds =>
            StunnedTimer == null ? 0f : (float)(StunnedTimer.Value.End - StunnedTimer.Value.Start).TotalSeconds;
        [ViewVariables] public float KnockdownSeconds =>
            KnockdownTimer == null ? 0f : (float)(KnockdownTimer.Value.End - KnockdownTimer.Value.Start).TotalSeconds;
        [ViewVariables] public float SlowdownSeconds =>
            SlowdownTimer == null ? 0f : (float)(SlowdownTimer.Value.End - SlowdownTimer.Value.Start).TotalSeconds;

        [ViewVariables] public bool AnyStunActive => Stunned || KnockedDown || SlowedDown;
        [ViewVariables] public bool Stunned => StunnedTimer != null;
        [ViewVariables] public bool KnockedDown => KnockdownTimer != null;
        [ViewVariables] public bool SlowedDown => SlowdownTimer != null;

        [DataField("stunCap")]
        protected float _stunCap = 20f;

        [DataField("knockdownCap")]
        protected float _knockdownCap = 20f;

        [DataField("slowdownCap")]
        protected float _slowdownCap = 20f;

        [DataField("helpInterval")]
        private float _helpInterval = 1f;

        private bool _canHelp = true;

        protected CancellationTokenSource StatusRemoveCancellation = new();

        [ViewVariables] protected float WalkModifierOverride = 0f;
        [ViewVariables] protected float RunModifierOverride = 0f;

        private float StunTimeModifier
        {
            get
            {
                var modifier = 1.0f;
                var components = Owner.GetAllComponents<IStunModifier>();

                foreach (var component in components)
                {
                    modifier *= component.StunTimeModifier;
                }

                return modifier;
            }
        }

        private float KnockdownTimeModifier
        {
            get
            {
                var modifier = 1.0f;
                var components = Owner.GetAllComponents<IStunModifier>();

                foreach (var component in components)
                {
                    modifier *= component.KnockdownTimeModifier;
                }

                return modifier;
            }
        }

        private float SlowdownTimeModifier
        {
            get
            {
                var modifier = 1.0f;
                var components = Owner.GetAllComponents<IStunModifier>();

                foreach (var component in components)
                {
                    modifier *= component.SlowdownTimeModifier;
                }

                return modifier;
            }
        }

        /// <summary>
        ///     Stuns the entity, disallowing it from doing many interactions temporarily.
        /// </summary>
        /// <param name="seconds">How many seconds the mob will stay stunned.</param>
        /// <returns>Whether or not the owner was stunned.</returns>
        public bool Stun(float seconds)
        {
            seconds = MathF.Min(StunnedSeconds + (seconds * StunTimeModifier), _stunCap);

            if (seconds <= 0f)
            {
                return false;
            }

            StunnedTimer = (_gameTiming.CurTime, _gameTiming.CurTime.Add(TimeSpan.FromSeconds(seconds)));

            SetAlert();
            OnStun();

            Dirty();

            return true;
        }

        protected virtual void OnStun() { }

        /// <summary>
        ///     Knocks down the mob, making it fall to the ground.
        /// </summary>
        /// <param name="seconds">How many seconds the mob will stay on the ground.</param>
        /// <returns>Whether or not the owner was knocked down.</returns>
        public bool Knockdown(float seconds)
        {
            seconds = MathF.Min(KnockdownSeconds + (seconds * KnockdownTimeModifier), _knockdownCap);

            if (seconds <= 0f)
            {
                return false;
            }

            KnockdownTimer = (_gameTiming.CurTime, _gameTiming.CurTime.Add(TimeSpan.FromSeconds(seconds)));;

            SetAlert();
            OnKnockdown();

            Dirty();

            return true;
        }

        protected virtual void OnKnockdown() { }

        /// <summary>
        ///     Applies knockdown and stun to the mob temporarily.
        /// </summary>
        /// <param name="seconds">How many seconds the mob will be paralyzed-</param>
        /// <returns>Whether or not the owner of this component was paralyzed-</returns>
        public bool Paralyze(float seconds)
        {
            return Stun(seconds) && Knockdown(seconds);
        }

        /// <summary>
        ///     Slows down the mob's walking/running speed temporarily
        /// </summary>
        /// <param name="seconds">How many seconds the mob will be slowed down</param>
        /// <param name="walkModifierOverride">Walk speed modifier. Set to 0 or negative for default value. (0.5f)</param>
        /// <param name="runModifierOverride">Run speed modifier. Set to 0 or negative for default value. (0.5f)</param>
        public void Slowdown(float seconds, float walkModifierOverride = 0f, float runModifierOverride = 0f)
        {
            seconds = MathF.Min(SlowdownSeconds + (seconds * SlowdownTimeModifier), _slowdownCap);

            if (seconds <= 0f)
                return;

            WalkModifierOverride = walkModifierOverride;
            RunModifierOverride = runModifierOverride;

            SlowdownTimer = (_gameTiming.CurTime, _gameTiming.CurTime.Add(TimeSpan.FromSeconds(seconds)));

            if (Owner.TryGetComponent(out MovementSpeedModifierComponent? movement))
                movement.RefreshMovementSpeedModifiers();

            SetAlert();
            Dirty();
        }

        private (TimeSpan, TimeSpan)? GetTimers()
        {
            // Don't do anything if no stuns are applied.
            if (!AnyStunActive)
                return null;

            TimeSpan start = TimeSpan.MaxValue, end = TimeSpan.MinValue;

            if (StunnedTimer != null)
            {
                if (StunnedTimer.Value.Start < start)
                    start = StunnedTimer.Value.Start;

                if (StunnedTimer.Value.End > end)
                    end = StunnedTimer.Value.End;
            }

            if (KnockdownTimer != null)
            {
                if (KnockdownTimer.Value.Start < start)
                    start = KnockdownTimer.Value.Start;

                if (KnockdownTimer.Value.End > end)
                    end = KnockdownTimer.Value.End;
            }

            if (SlowdownTimer != null)
            {
                if (SlowdownTimer.Value.Start < start)
                    start = SlowdownTimer.Value.Start;

                if (SlowdownTimer.Value.End > end)
                    end = SlowdownTimer.Value.End;
            }

            return (start, end);
        }

        private void SetAlert()
        {
            if (!Owner.TryGetComponent(out SharedAlertsComponent? status))
            {
                return;
            }

            var timers = GetTimers();

            if (timers == null)
                return;

            status.ShowAlert(AlertType.Stun, cooldown:timers);
            StatusRemoveCancellation.Cancel();
            StatusRemoveCancellation = new CancellationTokenSource();
        }

        protected virtual void OnInteractHand() { }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!_canHelp || !KnockedDown)
            {
                return false;
            }

            _canHelp = false;
            Owner.SpawnTimer((int) _helpInterval * 1000, () => _canHelp = true);

            KnockdownTimer = (KnockdownTimer!.Value.Start, KnockdownTimer.Value.End.Subtract(TimeSpan.FromSeconds(_helpInterval)));

            OnInteractHand();

            SetAlert();
            Dirty();

            return true;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new StunnableComponentState(StunnedTimer, KnockdownTimer, SlowdownTimer, WalkModifierOverride, RunModifierOverride);
        }

        protected virtual void OnKnockdownEnd()
        {
        }

        public void Update(float delta)
        {
            var curTime = _gameTiming.CurTime;

            if (StunnedTimer != null)
            {
                if (StunnedTimer.Value.End <= curTime)
                {
                    StunnedTimer = null;
                    Dirty();
                }
            }

            if (KnockdownTimer != null)
            {
                if (KnockdownTimer.Value.End <= curTime)
                {
                    OnKnockdownEnd();

                    KnockdownTimer = null;
                    Dirty();
                }
            }

            if (SlowdownTimer != null)
            {
                if (SlowdownTimer.Value.End <= curTime)
                {
                    if (Owner.TryGetComponent(out MovementSpeedModifierComponent? movement))
                    {
                        movement.RefreshMovementSpeedModifiers();
                    }

                    SlowdownTimer = null;
                    Dirty();
                }
            }

            if (AnyStunActive || !Owner.TryGetComponent(out SharedAlertsComponent? status) || !status.IsShowingAlert(AlertType.Stun))
                return;

            status.ClearAlert(AlertType.Stun);
        }

        #region ActionBlockers
        public bool CanMove() => (!Stunned);

        public bool CanInteract() => (!Stunned);

        public bool CanUse() => (!Stunned);

        public bool CanThrow() => (!Stunned);

        public bool CanSpeak() => true;

        public bool CanDrop() => (!Stunned);

        public bool CanPickup() => (!Stunned);

        public bool CanEmote() => true;

        public bool CanAttack() => (!Stunned);

        public bool CanEquip() => (!Stunned);

        public bool CanUnequip() => (!Stunned);
        public bool CanChangeDirection() => true;

        public bool CanShiver() => !Stunned;
        public bool CanSweat() => true;

        #endregion

        [ViewVariables]
        public float WalkSpeedModifier => (SlowedDown ? (WalkModifierOverride <= 0f ? 0.5f : WalkModifierOverride) : 1f);
        [ViewVariables]
        public float SprintSpeedModifier => (SlowedDown ? (RunModifierOverride <= 0f ? 0.5f : RunModifierOverride) : 1f);

        [Serializable, NetSerializable]
        protected sealed class StunnableComponentState : ComponentState
        {
            public (TimeSpan Start, TimeSpan End)? StunnedTimer { get; }
            public (TimeSpan Start, TimeSpan End)? KnockdownTimer { get; }
            public (TimeSpan Start, TimeSpan End)? SlowdownTimer { get; }
            public float WalkModifierOverride { get; }
            public float RunModifierOverride { get; }

            public StunnableComponentState(
                (TimeSpan Start, TimeSpan End)? stunnedTimer, (TimeSpan Start, TimeSpan End)? knockdownTimer,
                (TimeSpan Start, TimeSpan End)? slowdownTimer, float walkModifierOverride, float runModifierOverride)
            {
                StunnedTimer = stunnedTimer;
                KnockdownTimer = knockdownTimer;
                SlowdownTimer = slowdownTimer;
                WalkModifierOverride = walkModifierOverride;
                RunModifierOverride = runModifierOverride;
            }
        }
    }

    /// <summary>
    ///     This interface allows components to multiply the time in seconds of various stuns by a number.
    /// </summary>
    public interface IStunModifier
    {
        float StunTimeModifier => 1.0f;
        float KnockdownTimeModifier => 1.0f;
        float SlowdownTimeModifier => 1.0f;
    }
}
