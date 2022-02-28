using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Robust.Shared.IoC;

namespace Content.Shared.Chemistry.Components
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class MovespeedModifierMetabolismComponent : Component
    {
        [ViewVariables]
        public float WalkSpeedModifier { get; set; }

        [ViewVariables]
        public float SprintSpeedModifier { get; set; }

        /// <summary>
        /// When the current modifier is expected to end.
        /// </summary>
        [ViewVariables]
        public TimeSpan ModifierTimer { get; set; } = TimeSpan.Zero;

        public override ComponentState GetComponentState()
        {
            return new MovespeedModifierMetabolismComponentState(WalkSpeedModifier, SprintSpeedModifier, ModifierTimer);
        }

        [Serializable, NetSerializable]
        public sealed class MovespeedModifierMetabolismComponentState : ComponentState
        {
            public float WalkSpeedModifier { get; }
            public float SprintSpeedModifier { get; }
            public TimeSpan ModifierTimer { get; }

            public MovespeedModifierMetabolismComponentState(float walkSpeedModifier, float sprintSpeedModifier, TimeSpan modifierTimer)
            {
                WalkSpeedModifier = walkSpeedModifier;
                SprintSpeedModifier = sprintSpeedModifier;
                ModifierTimer = modifierTimer;
            }
        }
    }
}

