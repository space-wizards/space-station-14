#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs.State
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

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MobStateManagerComponentState state)
            {
                return;
            }

            _currentDamageState = state.DamageState;
            CurrentMobState?.ExitState(Owner);
            CurrentMobState = Behavior[CurrentDamageState];
            CurrentMobState.EnterState(Owner);
        }
    }
}
