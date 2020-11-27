using System.Collections.Generic;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.State
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMobStateManagerComponent))]
    public class MobStateManagerComponent : SharedMobStateManagerComponent
    {
        private readonly Dictionary<DamageState, IMobState> _behavior = new Dictionary<DamageState, IMobState>
        {
            {DamageState.Alive, new NormalState()},
            {DamageState.Critical, new CriticalState()},
            {DamageState.Dead, new DeadState()}
        };

        private DamageState _currentDamageState;

        protected override IReadOnlyDictionary<DamageState, IMobState> Behavior => _behavior;

        public override IMobState CurrentMobState { get; protected set; }

        public override DamageState CurrentDamageState
        {
            get => _currentDamageState;
            protected set
            {
                if (_currentDamageState == value)
                {
                    return;
                }

                if (_currentDamageState != DamageState.Invalid)
                {
                    CurrentMobState.ExitState(Owner);
                }

                _currentDamageState = value;
                CurrentMobState = Behavior[CurrentDamageState];
                CurrentMobState.EnterState(Owner);

                Dirty();
            }
        }

        public override void OnRemove()
        {
            // TODO: Might want to add an OnRemove() to IMobState since those are where these components are being used
            base.OnRemove();

            if (Owner.TryGetComponent(out ServerAlertsComponent status))
            {
                status.ClearAlert(AlertType.HumanHealth);
            }

            if (Owner.TryGetComponent(out ServerOverlayEffectsComponent overlay))
            {
                overlay.ClearOverlays();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new MobStateManagerComponentState(CurrentDamageState);
        }
    }
}
