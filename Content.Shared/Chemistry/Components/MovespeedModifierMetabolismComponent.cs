using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Threading;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Robust.Shared.IoC;

namespace Content.Shared.Chemistry.Components
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class MovespeedModifierMetabolismComponent : Component, IMoveSpeedModifier
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ViewVariables]
        public override string Name => "MovespeedModifierMetabolismComponent";

        [ViewVariables]
        public float WalkSpeedModifier { get; set; }

        [ViewVariables]
        public float SprintSpeedModifier { get; set; }

        [ViewVariables]
        public int EffectTime { get; set; }

        private CancellationTokenSource? _cancellation;

        public (TimeSpan Start, TimeSpan End)? ModifierTimer { get; set; }

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

        public void Update(float delta)
        {
            var curTime = _gameTiming.CurTime;

            if (ModifierTimer != null)
            {
                if (ModifierTimer.Value.End <= curTime)
                {
                    ModifierTimer = null;
                    ResetModifiers();
                    Dirty();
                }
            }
        }

        /// <summary>
        /// Perpetuate the modifiers further.
        /// </summary>
        public void ResetTimer()
        {
            _cancellation?.Cancel();
            _cancellation = new CancellationTokenSource();
            Owner.SpawnTimer(EffectTime, ResetModifiers, _cancellation.Token);

            ModifierTimer = (_gameTiming.CurTime, _gameTiming.CurTime.Add(TimeSpan.FromSeconds(EffectTime / 1000))); // EffectTime is milliseconds, TimeSpan.FromSeconds() is just seconds
            Dirty();
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not MovespeedModifierMetabolismComponentState state) return;

            if (state.WalkSpeedModifier.Equals(WalkSpeedModifier) &&
                state.SprintSpeedModifier.Equals(SprintSpeedModifier)) return;

            WalkSpeedModifier = state.WalkSpeedModifier;
            SprintSpeedModifier = state.SprintSpeedModifier;
            ModifierTimer = state.ModifierTimer;

            if (Owner.TryGetComponent(out MovementSpeedModifierComponent? modifier))
            {
                modifier.RefreshMovementSpeedModifiers();
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new MovespeedModifierMetabolismComponentState(WalkSpeedModifier, SprintSpeedModifier, ModifierTimer);
        }

        [Serializable, NetSerializable]
        public class MovespeedModifierMetabolismComponentState : ComponentState
        {
            public float WalkSpeedModifier { get; }
            public float SprintSpeedModifier { get; }
            public (TimeSpan Start, TimeSpan End)? ModifierTimer { get; }

            public MovespeedModifierMetabolismComponentState(float walkSpeedModifier, float sprintSpeedModifier, (TimeSpan Start, TimeSpan End)? modifierTimer)
            {
                WalkSpeedModifier = walkSpeedModifier;
                SprintSpeedModifier = sprintSpeedModifier;
                ModifierTimer = modifierTimer;
            }
        }
    }
}

