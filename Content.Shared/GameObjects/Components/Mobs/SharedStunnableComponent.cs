#nullable enable
using System;
using System.Threading;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedStunnableComponent : Component, IMoveSpeedModifier, IActionBlocker, IInteractHand
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public sealed override string Name => "Stunnable";
        public override uint? NetID => ContentNetIDs.STUNNABLE;

        protected TimeSpan? LastStun;

        [ViewVariables] protected TimeSpan? StunStart => LastStun;

        [ViewVariables]
        protected TimeSpan? StunEnd => LastStun == null
            ? (TimeSpan?) null
            : _gameTiming.CurTime +
              (TimeSpan.FromSeconds(Math.Max(StunnedTimer, Math.Max(KnockdownTimer, SlowdownTimer))));

        private bool _canHelp = true;

        [DataField("stunCap")]
        protected float _stunCap = 20f;

        [DataField("knockdownCap")]
        protected float _knockdownCap = 20f;

        [DataField("slowdownCap")]
        protected float _slowdownCap = 20f;

        private float _helpKnockdownRemove = 1f;

        [DataField("helpInterval")]
        private float _helpInterval = 1f;

        protected float StunnedTimer;
        protected float KnockdownTimer;
        protected float SlowdownTimer;

        [DataField("stunAlertId")] private string _stunAlertId = "stun";

        protected CancellationTokenSource StatusRemoveCancellation = new();

        [ViewVariables] protected float WalkModifierOverride = 0f;
        [ViewVariables] protected float RunModifierOverride = 0f;

        [ViewVariables] public bool Stunned => StunnedTimer > 0f;
        [ViewVariables] public bool KnockedDown => KnockdownTimer > 0f;
        [ViewVariables] public bool SlowedDown => SlowdownTimer > 0f;

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
            seconds = MathF.Min(StunnedTimer + (seconds * StunTimeModifier), _stunCap);

            if (seconds <= 0f)
            {
                return false;
            }

            StunnedTimer = seconds;
            LastStun = _gameTiming.CurTime;

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
            seconds = MathF.Min(KnockdownTimer + (seconds * KnockdownTimeModifier), _knockdownCap);

            if (seconds <= 0f)
            {
                return false;
            }

            KnockdownTimer = seconds;
            LastStun = _gameTiming.CurTime;

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
            seconds = MathF.Min(SlowdownTimer + (seconds * SlowdownTimeModifier), _slowdownCap);

            if (seconds <= 0f)
                return;

            WalkModifierOverride = walkModifierOverride;
            RunModifierOverride = runModifierOverride;

            SlowdownTimer = seconds;
            LastStun = _gameTiming.CurTime;

            if (Owner.TryGetComponent(out MovementSpeedModifierComponent? movement))
                movement.RefreshMovementSpeedModifiers();

            SetAlert();
            Dirty();
        }

        private void SetAlert()
        {
            if (!Owner.TryGetComponent(out SharedAlertsComponent? status))
            {
                return;
            }

            status.ShowAlert(AlertType.Stun, cooldown:
                (StunStart == null || StunEnd == null) ? default : (StunStart.Value, StunEnd.Value));
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

            KnockdownTimer -= _helpKnockdownRemove;

            OnInteractHand();

            SetAlert();
            Dirty();

            return true;
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
            public float StunnedTimer { get; }
            public float KnockdownTimer { get; }
            public float SlowdownTimer { get; }
            public float WalkModifierOverride { get; }
            public float RunModifierOverride { get; }

            public StunnableComponentState(float stunnedTimer, float knockdownTimer, float slowdownTimer, float walkModifierOverride, float runModifierOverride) : base(ContentNetIDs.STUNNABLE)
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
