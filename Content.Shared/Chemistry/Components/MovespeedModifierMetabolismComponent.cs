using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Threading;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class MovespeedModifierMetabolismComponent : Component, IMoveSpeedModifier
    {
        [ViewVariables]
        public override string Name => "MovespeedModifierMetabolismComponent";

        [ViewVariables]
        public float WalkSpeedModifier { get; set; }

        [ViewVariables]
        public float SprintSpeedModifier { get; set; }

        [ViewVariables]
        public int EffectTime { get; set; }

        private CancellationTokenSource? _cancellation;

        public void ResetModifiers()
        {
            WalkSpeedModifier = 1;
            SprintSpeedModifier = 1;

            if (Owner.TryGetComponent(out MovementSpeedModifierComponent? modifier))
            {
                modifier.RefreshMovementSpeedModifiers();
            }

            _cancellation?.Cancel();
            Dirty();
        }

        /// <summary>
        /// Perpetuate the modifiers further.
        /// </summary>
        public void ResetTimer()
        {
            _cancellation?.Cancel();
            _cancellation = new CancellationTokenSource();
            Owner.SpawnTimer(EffectTime, ResetModifiers, _cancellation.Token);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not MovespeedModifierMetabolismComponentState state) return;

            if (state.WalkSpeedModifier.Equals(WalkSpeedModifier) &&
                state.SprintSpeedModifier.Equals(SprintSpeedModifier)) return;

            WalkSpeedModifier = state.WalkSpeedModifier;
            SprintSpeedModifier = state.SprintSpeedModifier;
            if (Owner.TryGetComponent(out MovementSpeedModifierComponent? modifier))
            {
                modifier.RefreshMovementSpeedModifiers();
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new MovespeedModifierMetabolismComponentState(WalkSpeedModifier, SprintSpeedModifier);
        }

        [Serializable, NetSerializable]
        public class MovespeedModifierMetabolismComponentState : ComponentState
        {
            public float WalkSpeedModifier { get; }
            public float SprintSpeedModifier { get; }
            public MovespeedModifierMetabolismComponentState(float walkSpeedModifier, float sprintSpeedModifier)
            {
                WalkSpeedModifier = walkSpeedModifier;
                SprintSpeedModifier = sprintSpeedModifier;
            }
        }
    }
}

