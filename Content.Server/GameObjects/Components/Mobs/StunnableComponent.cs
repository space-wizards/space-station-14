using System;
using System.Threading;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Mobs;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timers;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class StunnableComponent : Component, IActionBlocker, IAttackHand, IMoveSpeedModifier
    {
        public override string Name => "Stunnable";

#pragma warning disable 649
        [Dependency] private IEntitySystemManager _entitySystemManager;
        [Dependency] private IGameTiming _gameTiming;
#pragma warning restore 649

        private TimeSpan? _lastStun;

        [ViewVariables]
        public TimeSpan? StunStart => _lastStun;

        [ViewVariables]
        public TimeSpan? StunEnd => _lastStun == null
            ? (TimeSpan?) null
            : _gameTiming.CurTime + TimeSpan.FromSeconds(_stunnedTimer + _knockdownTimer + _slowdownTimer);

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

        private float _walkModifierOverride = 0f;
        private float _runModifierOverride = 0f;
        private readonly string[] _texturesStunOverlay = new string[StunLevels];

        [ViewVariables] public bool Stunned => _stunnedTimer > 0f;
        [ViewVariables] public bool KnockedDown => _knockdownTimer > 0f;
        [ViewVariables] public bool SlowedDown => _slowdownTimer > 0f;
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
        }

        public override void Initialize()
        {
            base.Initialize();

            for (var i = 0; i < StunLevels; i++)
            {
                _texturesStunOverlay[i] = $"/Textures/UserInterface/Inventory/cooldown-{i}.png";
            }
        }

        /// <summary>
        ///     Stuns the entity, disallowing it from doing many interactions temporarily.
        /// </summary>
        /// <param name="seconds">How many seconds the mob will stay stunned</param>
        public void Stun(float seconds)
        {
            seconds = Math.Min(_stunnedTimer + (seconds * StunTimeModifier), _stunCap);

            if (seconds <= 0f)
                return;

            StandingStateHelper.DropAllItemsInHands(Owner);

            _stunnedTimer = seconds;
            _lastStun = _gameTiming.CurTime;
        }

        /// <summary>
        ///     Knocks down the mob, making it fall to the ground.
        /// </summary>
        /// <param name="seconds">How many seconds the mob will stay on the ground</param>
        public void Knockdown(float seconds)
        {
            seconds = MathF.Min(_knockdownTimer + (seconds * KnockdownTimeModifier), _knockdownCap);

            if (seconds <= 0f)
                return;

            StandingStateHelper.Down(Owner);

            _knockdownTimer = seconds;
            _lastStun = _gameTiming.CurTime;
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

            _walkModifierOverride = walkModifierOverride;
            _runModifierOverride = runModifierOverride;

            _slowdownTimer = seconds;
            _lastStun = _gameTiming.CurTime;

            if(Owner.TryGetComponent(out MovementSpeedModifierComponent movement))
                movement.RefreshMovementSpeedModifiers();
        }

        /// <summary>
        ///     Used when
        /// </summary>
        public void CancelAll()
        {
            _knockdownTimer = 0f;
            _stunnedTimer = 0f;
        }

        public bool AttackHand(AttackHandEventArgs eventArgs)
        {
            if (!_canHelp || !KnockedDown)
                return false;

            _canHelp = false;
            Timer.Spawn(((int)_helpInterval*1000), () => _canHelp = true);

            _entitySystemManager.GetEntitySystem<AudioSystem>()
                .Play("/Audio/effects/thudswoosh.ogg", Owner, AudioHelpers.WithVariation(0.25f));

            _knockdownTimer -= _helpKnockdownRemove;

            return true;
        }

        public void Update(float delta)
        {
            if (Stunned)
            {
                _stunnedTimer -= delta;

                if (_stunnedTimer <= 0)
                {
                    _stunnedTimer = 0f;
                }
            }

            if (KnockedDown)
            {
                _knockdownTimer -= delta;

                if (_knockdownTimer <= 0f)
                {
                    StandingStateHelper.Standing(Owner);

                    _knockdownTimer = 0f;
                }
            }

            if (SlowedDown)
            {
                _slowdownTimer -= delta;

                if (_slowdownTimer <= 0f)
                {
                    _slowdownTimer = 0f;

                    if(Owner.TryGetComponent(out MovementSpeedModifierComponent movement))
                        movement.RefreshMovementSpeedModifiers();
                }
            }

            if (!_lastStun.HasValue || !StunEnd.HasValue || !Owner.TryGetComponent(out ServerStatusEffectsComponent status))
                return;

            var start = _lastStun.Value;
            var end = StunEnd.Value;

            var length = (end - start).TotalSeconds;
            var progress = (_gameTiming.CurTime - start).TotalSeconds;
            var ratio = (float)(progress / length);

            var textureIndex = CalculateStunLevel(ratio);
            if (textureIndex == StunLevels)
            {
                _lastStun = null;
                status.RemoveStatus(StatusEffect.Stun);
            }
            else
            {
                status.ChangeStatus(StatusEffect.Stun, _texturesStunOverlay[textureIndex]);
            }
        }

        private static int CalculateStunLevel(float stunValue)
        {
            var val = stunValue.Clamp(0, 1);
            val *= StunLevels;
            return (int)Math.Floor(val);
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
        #endregion

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

        public float WalkSpeedModifier => (SlowedDown ? (_walkModifierOverride <= 0f ? 0.5f : _walkModifierOverride) : 1f);
        public float SprintSpeedModifier => (SlowedDown ? (_runModifierOverride <= 0f ? 0.5f : _runModifierOverride) : 1f);
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
