using Content.Shared.Movement.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable
{
    [RegisterComponent]
    [NetworkedComponent]
    [Friend(typeof(SharedStunSystem))]
    public class SlowedDownComponent : Component, IMoveSpeedModifier
    {
        public override string Name => "SlowedDown";

        public float SprintSpeedModifier { get; set;  }
        public float WalkSpeedModifier { get; set;  }
    }
}
