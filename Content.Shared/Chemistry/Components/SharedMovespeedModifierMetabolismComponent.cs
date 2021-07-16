using Content.Shared.Movement.Components;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;
using System;
using System.Threading;

namespace Content.Shared.Chemistry.Components
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    public class SharedMovespeedModifierMetabolismComponent : Component, IMoveSpeedModifier
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

        public (TimeSpan Start, TimeSpan End)? ModifierTimer { get; set; }

        public void ResetModifiers()
        {
            WalkSpeedModifier = 1;
            SprintSpeedModifier = 1;
            var movement = Owner.GetComponent<MovementSpeedModifierComponent>();
            movement.RefreshMovementSpeedModifiers();
            ModifierTimer = null;
            Dirty();
        }
        public void ResetTimer()
        {
            ModifierTimer = (_gameTiming.CurTime, _gameTiming.CurTime.Add(TimeSpan.FromSeconds(EffectTime/1000)));
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

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new MovespeedModifierMetabolismComponentState(WalkSpeedModifier, SprintSpeedModifier, ModifierTimer);
        }

        [Serializable, NetSerializable]
        public class MovespeedModifierMetabolismComponentState : ComponentState
        {
            public float WalkSpeedModifier { get; }
            public float SprintSpeedModifier { get; }
            public (TimeSpan Start, TimeSpan End)? ModifierTimer { get; set; }

            public MovespeedModifierMetabolismComponentState(float walkSpeedModifier, float sprintSpeedModifier, (TimeSpan Start, TimeSpan End)? modifierTimer): base(ContentNetIDs.METABOLISM_SPEEDCHANGE)
            {
                WalkSpeedModifier = walkSpeedModifier;
                SprintSpeedModifier = sprintSpeedModifier;
                ModifierTimer = modifierTimer;
            }
        }


        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MovespeedModifierMetabolismComponentState state)
            {
                return;
            }

            WalkSpeedModifier = state.WalkSpeedModifier;
            SprintSpeedModifier = state.SprintSpeedModifier;

            ResetTimer();

            Owner.TryGetComponent(out MovementSpeedModifierComponent? movement);
            movement?.RefreshMovementSpeedModifiers();
        }
    }
}

