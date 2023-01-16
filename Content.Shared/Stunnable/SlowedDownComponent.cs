using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable
{
    [RegisterComponent]
    [NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(SharedStunSystem))]
    public sealed class SlowedDownComponent : Component
    {
        [AutoNetworkedField]
        public float SprintSpeedModifier { get; set; } = 0.5f;
        [AutoNetworkedField]
        public float WalkSpeedModifier { get; set; } = 0.5f;
    }
}
