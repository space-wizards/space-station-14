using System;
using System.Threading;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timers;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class StunnableComponent : Component, IActionBlocker
    {
        [Dependency] private ITimerManager _timerManager;

        private bool _stunned = false;
        private bool _knocked = false;

        private int _stunCapMs = 20000;
        private int _knockdownCapMs = 20000;

        private Timer _stunTimer;
        private Timer _knockdownTimer;

        private CancellationTokenSource _stunTimerCancellation;
        private CancellationTokenSource _knockdownTimerCancellation;

        public override string Name => "Stunnable";

        [ViewVariables] public bool Stunned => _stunned;
        [ViewVariables] public bool KnockedDown => _knocked;

        public void Stun(int milliseconds)
        {
            if (_stunTimer != null)
            {
                _stunTimerCancellation.Cancel();
                milliseconds += _stunTimer.Time;
            }

            milliseconds = Math.Min(milliseconds, _stunCapMs);

            DropItemsInHands();

            _stunned = true;
            _stunTimerCancellation = new CancellationTokenSource();
            _stunTimer = new Timer(milliseconds, false, OnStunTimerFired);
            _timerManager.AddTimer(_stunTimer, _stunTimerCancellation.Token);
        }

        public override void Initialize()
        {
            base.Initialize();
            Timer.Spawn(10000, () => Paralyze(5000));
        }

        public void Knockdown(int milliseconds)
        {
            if (_knockdownTimer != null)
            {
                _knockdownTimerCancellation.Cancel();
                milliseconds += _knockdownTimer.Time;
            }

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                var state = SharedSpeciesComponent.MobState.Down;
                appearance.SetData(SharedSpeciesComponent.MobVisuals.RotationState, state);
            }

            milliseconds = Math.Min(milliseconds, _knockdownCapMs);

            DropItemsInHands();

            _knocked = true;
            _knockdownTimerCancellation = new CancellationTokenSource();
            _knockdownTimer = new Timer(milliseconds, false, OnKnockdownTimerFired);
            _timerManager.AddTimer(_knockdownTimer, _knockdownTimerCancellation.Token);
        }

        private void DropItemsInHands()
        {
            if (!Owner.TryGetComponent(out IHandsComponent hands)) return;

            foreach (var heldItem in hands.GetAllHeldItems())
            {
                hands.Drop(heldItem.Owner);
            }
        }

        private void OnStunTimerFired()
        {
            _stunned = false;
            _stunTimer = null;
            _stunTimerCancellation = null;
        }

        private void OnKnockdownTimerFired()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                var state = SharedSpeciesComponent.MobState.Stand;
                appearance.SetData(SharedSpeciesComponent.MobVisuals.RotationState, state);
            }

            _knocked = false;
            _knockdownTimer = null;
            _knockdownTimerCancellation = null;
        }

        public void Paralyze(int milliseconds)
        {
            Stun(milliseconds);
            Knockdown(milliseconds);
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
        #endregion
    }
}
