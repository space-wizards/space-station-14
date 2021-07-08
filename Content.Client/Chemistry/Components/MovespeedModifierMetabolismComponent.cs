using Content.Shared.Chemistry.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using System.Threading;

namespace Content.Server.GameObjects.Components.Chemistry
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    [RegisterComponent]
    public class MovespeedModifierMetabolismComponent : SharedMovespeedModifierMetabolismComponent
    {

        private CancellationTokenSource? _cancellation = new();

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MovespeedModifierMetabolismComponentState state)
            {
                return;
            }

            WalkSpeedModifier = state.WalkSpeedModifier;
            SprintSpeedModifier = state.SprintSpeedModifier;

            _cancellation = new CancellationTokenSource();

            Owner.SpawnTimer(EffectTime, ResetModifiers, _cancellation.Token);

            Owner.TryGetComponent(out MovementSpeedModifierComponent? movement);
            movement?.RefreshMovementSpeedModifiers();
            

        }
    }
}
