using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System.Threading;

namespace Content.Server.GameObjects.Components.Chemistry
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    [RegisterComponent]
    class MovespeedModifierMetabolismClientComponent : Component
    {
        [ViewVariables]
        public override string Name => "MovespeedModifierMetabolismClientComponent";

        [ViewVariables]
        public float WalkSpeedModifier { get; set; }

        [ViewVariables]
        public float SprintSpeedModifier { get; set; }

        [ViewVariables]
        public int EffectTime { get; set; }

        private CancellationTokenSource? _cancellation;

        private void ResetModifiers()
        {
            WalkSpeedModifier = 1;
            SprintSpeedModifier = 1;
            var movement = Owner.GetComponent<MovementSpeedModifierComponent>();
            movement.RefreshMovementSpeedModifiers();
            _cancellation?.Cancel();
        }

        public void ResetTimer()
        {
            _cancellation?.Cancel();
            _cancellation = new CancellationTokenSource();
            Owner.SpawnTimer(EffectTime, ResetModifiers, _cancellation.Token);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            //if (curState is not MovespeedModifierMetabolismComponentState state)
            //{
            //    return;
            //}


            //WalkSpeedModifier = state.WalkSpeedModifier;
            //SprintSpeedModifier = state.SprintSpeedModifier;

            if (Owner.TryGetComponent(out MovementSpeedModifierComponent? movement))
            {
                movement.RefreshMovementSpeedModifiers();
            }

        }
    }
}
