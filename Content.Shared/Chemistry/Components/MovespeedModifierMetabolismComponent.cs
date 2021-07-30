using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
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

        public (TimeSpan Start, TimeSpan End)? ModifierTimer { get; set; }

        /// <summary>
        /// Perpetuate the modifiers further.
        /// </summary>

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

