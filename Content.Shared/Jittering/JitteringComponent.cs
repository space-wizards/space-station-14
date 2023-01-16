using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Jittering
{
    [Access(typeof(SharedJitteringSystem))]
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed class JitteringComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public float Amplitude { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public float Frequency { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LastJitter { get; set; }
    }
}
