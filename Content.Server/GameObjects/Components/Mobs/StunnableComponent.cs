using System;
using System.Threading;
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
    public class StunnableComponent : Component, IActionBlocker, IAttackHand
    {
        [Dependency] private IEntitySystemManager _entitySystemManager;
        [Dependency] private ITimerManager _timerManager;

        private bool _stunned = false;
        private bool _knocked = false;
        private bool _canHelp = true;

        private float _stunCap = 20f;
        private float _knockdownCap = 20f;
        private float _helpKnockdownRemove = 1f;
        private float _helpInterval = 1f;

        private float _stunnedTimer = 0f;
        private float _knockdownTimer = 0f;

        public override string Name => "Stunnable";

        [ViewVariables] public bool Stunned => _stunned;
        [ViewVariables] public bool KnockedDown => _knocked;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _stunCap, "stunCap", 20f);
            serializer.DataField(ref _knockdownCap, "knockdownCap", 20f);
            serializer.DataField(ref _helpInterval, "helpInterval", 1f);
            serializer.DataField(ref _helpKnockdownRemove, "helpKnockdownRemove", 1f);
        }

        public void Stun(float seconds)
        {
            seconds = Math.Min(seconds + _stunnedTimer, _stunCap);

            StandingStateHelper.DropAllItemsInHands(Owner);

            _stunned = true;
            _stunnedTimer = seconds;
        }

        public void Knockdown(float seconds)
        {
            seconds = MathF.Min(_knockdownTimer + seconds, _knockdownCap);

            StandingStateHelper.Down(Owner);

            _knocked = true;
            _knockdownTimer = seconds;
        }

        public void Paralyze(float seconds)
        {
            Stun(seconds);
            Knockdown(seconds);
        }

        /// <summary>
        ///     Used when
        /// </summary>
        public void CancelAll()
        {
            _knocked = false;
            _stunned = false;

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
            if (_knocked)
            {
                _knockdownTimer -= delta;

                if (_knockdownTimer <= 0f)
                {
                    StandingStateHelper.Standing(Owner);

                    _knocked = false;
                }
            }

            if (_stunned)
            {
                _stunnedTimer -= delta;

                if (_stunnedTimer <= 0)
                {
                    _stunned = false;
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
    }
}
