using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Jittering
{
    [Access(typeof(SharedJitteringSystem))]
    [RegisterComponent, NetworkedComponent]
    public sealed partial class JitteringComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public float Amplitude { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Frequency { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LastJitter { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class JitteringComponentState : ComponentState
    {
        public float Amplitude { get; }
        public float Frequency { get; }

        public JitteringComponentState(float amplitude, float frequency)
        {
            Amplitude = amplitude;
            Frequency = frequency;
        }
    }
}
