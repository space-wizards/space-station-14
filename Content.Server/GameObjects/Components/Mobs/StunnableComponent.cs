using System;
using System.Threading;
using Content.Server.Mobs;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Interfaces.GameObjects.Components;
using Math = CannyFastMath.Math;
using MathF = CannyFastMath.MathF;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStunnableComponent))]
    public class StunnableComponent : SharedStunnableComponent, IInteractHand
    {
#pragma warning disable 649
        [Dependency] private IGameTiming _gameTiming;
#pragma warning restore 649

        private TimeSpan? _lastStun;

        [ViewVariables] public TimeSpan? StunStart => _lastStun;

        [ViewVariables]
        public TimeSpan? StunEnd => _lastStun == null
            ? (TimeSpan?) null
            : _gameTiming.CurTime +
              (TimeSpan.FromSeconds(Math.Max(_stunnedTimer, Math.Max(_knockdownTimer, _slowdownTimer))));

        private const int StunLevels = 8;

        private bool _canHelp = true;
        private float _stunCap = 20f;
        private float _knockdownCap = 20f;
        private float _slowdownCap = 20f;
        private float _helpKnockdownRemove = 1f;
        private float _helpInterval = 1f;

        private float _stunnedTimer = 0f;
        private float _knockdownTimer = 0f;
        private float _slowdownTimer = 0f;

        private string _stunTexture;
        private CancellationTokenSource _statusRemoveCancellation = new CancellationTokenSource();

        [ViewVariables] public override bool Stunned => _stunnedTimer > 0f;
        [ViewVariables] public override bool KnockedDown => _knockdownTimer > 0f;
        [ViewVariables] public override bool SlowedDown => _slowdownTimer > 0f;
        [ViewVariables] public float StunCap => _stunCap;
        [ViewVariables] public float KnockdownCap => _knockdownCap;
        [ViewVariables] public float SlowdownCap => _slowdownCap;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _stunCap, "stunCap", 20f);
            serializer.DataField(ref _knockdownCap, "knockdownCap", 20f);
            serializer.DataField(ref _slowdownCap, "slowdownCap", 20f);
            serializer.DataField(ref _helpInterval, "helpInterval", 1f);
            serializer.DataField(ref _helpKnockdownRemove, "helpKnockdownRemove", 1f);
            serializer.DataField(ref _stunTexture, "stunTexture",
                "/Textures/Objects/Weapons/Melee/stunbaton.rsi/stunbaton_off.png");
        }

        /// <summary>
        ///     Stuns the entity, disallowing it from doing many interactions temporarily.
        /// </summary>
        /// <param name="seconds">How many seconds the mob will stay stunned</param>
        public void Stun(float seconds)
        {
            seconds = MathF.Min(_stunnedTimer + (seconds * StunTimeModifier), _stunCap);

            if (seconds <= 0f)
                return;

            StandingStateHelper.DropAllItemsInHands(Owner, false);

            _stunnedTimer = seconds;
            _lastStun = _gameTiming.CurTime;

            SetStatusEffect();
            Dirty();
        }

        /// <summary>
        ///     Knocks down the mob, making it fall to the ground.
        /// </summary>
        /// <param name="seconds">How many seconds the mob will stay on the ground</param>
        public void Knockdown(float seconds)
        {
            seconds = MathF.Min(_knockdownTimer + (seconds * KnockdownTimeModifier), _knockdownCap);

            if (seconds <= 0f)
            {
                return;
            }

            StandingStateHelper.Down(Owner);

            _knockdownTimer = seconds;
            _lastStun = _gameTiming.CurTime;

            SetStatusEffect();
            Dirty();
        }

        /// <summary>
        ///     Applies knockdown and stun to the mob temporarily
        /// </summary>
        /// <param name="seconds">How many seconds the mob will be paralyzed</param>
        public void Paralyze(float seconds)
        {
            Stun(seconds);
            Knockdown(seconds);
        }

        /// <summary>
        ///     Slows down the mob's walking/running speed temporarily
        /// </summary>
        /// <param name="seconds">How many seconds the mob will be slowed down</param>
        /// <param name="walkModifierOverride">Walk speed modifier. Set to 0 or negative for default value. (0.5f)</param>
        /// <param name="runModifierOverride">Run speed modifier. Set to 0 or negative for default value. (0.5f)</param>
        public void Slowdown(float seconds, float walkModifierOverride = 0f, float runModifierOverride = 0f)
        {
            seconds = MathF.Min(_slowdownTimer + (seconds * SlowdownTimeModifier), _slowdownCap);

            if (seconds <= 0f)
                return;

            WalkModifierOverride = walkModifierOverride;
            RunModifierOverride = runModifierOverride;

            _slowdownTimer = seconds;
            _lastStun = _gameTiming.CurTime;

            if (Owner.TryGetComponent(out MovementSpeedModifierComponent movement))
                movement.RefreshMovementSpeedModifiers();

            SetStatusEffect();
            Dirty();
        }

        /// <summary>
        ///     Used when
        /// </summary>
        public void CancelAll()
        {
            _knockdownTimer = 0f;
            _stunnedTimer = 0f;
            Dirty();
        }

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!_canHelp || !KnockedDown)
                return false;

            _canHelp = false;
            Timer.Spawn(((int) _helpInterval * 1000), () => _canHelp = true);

            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity("/Audio/Effects/thudswoosh.ogg", Owner, AudioHelpers.WithVariation(0.25f));

            _knockdownTimer -= _helpKnockdownRemove;

            SetStatusEffect();

            Dirty();
            return true;
        }

        private void SetStatusEffect()
        {
            if (!Owner.TryGetComponent(out ServerStatusEffectsComponent status))
                return;

            status.ChangeStatusEffect(StatusEffect.Stun, _stunTexture,
                (StunStart == null || StunEnd == null) ? default : (StunStart.Value, StunEnd.Value));
            _statusRemoveCancellation.Cancel();
            _statusRemoveCancellation = new CancellationTokenSource();
        }

        public void ResetStuns()
        {
            _stunnedTimer = 0f;
            _slowdownTimer = 0f;

            if (KnockedDown)
                StandingStateHelper.Standing(Owner);

            _knockdownTimer = 0f;
        }

        public void Update(float delta)
        {
            if (Stunned)
            {
                _stunnedTimer -= delta;

                if (_stunnedTimer <= 0)
                {
                    _stunnedTimer = 0f;
                    Dirty();
                }
            }

            if (KnockedDown)
            {
                _knockdownTimer -= delta;

                if (_knockdownTimer <= 0f)
                {
                    StandingStateHelper.Standing(Owner);

                    _knockdownTimer = 0f;
                    Dirty();
                }
            }

            if (SlowedDown)
            {
                _slowdownTimer -= delta;

                if (_slowdownTimer <= 0f)
                {
                    _slowdownTimer = 0f;

                    if (Owner.TryGetComponent(out MovementSpeedModifierComponent movement))
                        movement.RefreshMovementSpeedModifiers();
                    Dirty();
                }
            }

            if (!StunStart.HasValue || !StunEnd.HasValue ||
                !Owner.TryGetComponent(out ServerStatusEffectsComponent status))
                return;

            var start = StunStart.Value;
            var end = StunEnd.Value;

            var length = (end - start).TotalSeconds;
            var progress = (_gameTiming.CurTime - start).TotalSeconds;

            if (progress >= length)
            {
                Timer.Spawn(250, () => status.RemoveStatusEffect(StatusEffect.Stun), _statusRemoveCancellation.Token);
                _lastStun = null;
            }
        }

        public float StunTimeModifier
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

        public float KnockdownTimeModifier
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

        public float SlowdownTimeModifier
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

        public override ComponentState GetComponentState()
        {
            return new StunnableComponentState(Stunned, KnockedDown, SlowedDown, WalkModifierOverride,
                RunModifierOverride);
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
