using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable
{
    [RegisterComponent]
    [NetworkedComponent]
    [Access(typeof(SharedStunSystem))]
    public sealed class SlowedDownComponent : Component
    {
        public float SprintSpeedModifier { get; set; } = 0.5f;
        public float WalkSpeedModifier { get; set; } = 0.5f;
    }

    [Serializable, NetSerializable]
    public sealed class SlowedDownComponentState : ComponentState
    {
        public float SprintSpeedModifier { get; set;  }
        public float WalkSpeedModifier { get; set;  }

        public SlowedDownComponentState(float sprintSpeedModifier, float walkSpeedModifier)
        {
            SprintSpeedModifier = sprintSpeedModifier;
            WalkSpeedModifier = walkSpeedModifier;
        }
    }
}
