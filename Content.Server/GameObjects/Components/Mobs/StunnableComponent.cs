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
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class StunnableComponent : Component, IActionBlocker, IAttackHand, IMoveSpeedModifier
    {
        [Dependency] private IEntitySystemManager _entitySystemManager;
        [Dependency] private ITimerManager _timerManager;

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

        public override string Name => "Stunnable";

        [ViewVariables] public bool Stunned => _stunnedTimer > 0f;
        [ViewVariables] public bool KnockedDown => _knockdownTimer > 0f;
        [ViewVariables] public bool SlowedDown => _slowdownTimer > 0f;
        [ViewVariables] public float StunCap => _stunCap;
        [ViewVariables] public float KnockdownCap => _knockdownCap;
        [ViewVariables] public float SlowdownCap => _slowdownCap;

        public override void Initialize()
        {
            base.Initialize();

            Timer.Spawn(1000, () => Slowdown(20f));
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _stunCap, "stunCap", 20f);
            serializer.DataField(ref _knockdownCap, "knockdownCap", 20f);
            serializer.DataField(ref _slowdownCap, "slowdownCap", 20f);
            serializer.DataField(ref _helpInterval, "helpInterval", 1f);
            serializer.DataField(ref _helpKnockdownRemove, "helpKnockdownRemove", 1f);
        }

        public void Stun(float seconds)
        {
            seconds = Math.Min(seconds + _stunnedTimer, _stunCap);

            StandingStateHelper.DropAllItemsInHands(Owner);

            _stunnedTimer = seconds;
        }

        public void Knockdown(float seconds)
        {
            seconds = MathF.Min(_knockdownTimer + seconds, _knockdownCap);

            StandingStateHelper.Down(Owner);

            _knockdownTimer = seconds;
        }

        public void Paralyze(float seconds)
        {
            Stun(seconds);
            Knockdown(seconds);
        }

        public void Slowdown(float seconds, float walkModifierOverride = 0f, float runModifierOverride = 0f)
        {
            seconds = MathF.Min(_slowdownTimer + seconds, _slowdownCap);

            _walkModifierOverride = walkModifierOverride;
            _runModifierOverride = runModifierOverride;

            _slowdownTimer = seconds;

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

            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>()
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

        public float WalkSpeedModifier => (SlowedDown ? (_walkModifierOverride <= 0f ? 0.5f : _walkModifierOverride) : 1f);
        public float SprintSpeedModifier => (SlowedDown ? (_runModifierOverride <= 0f ? 0.5f : _runModifierOverride) : 1f);
    }
}
