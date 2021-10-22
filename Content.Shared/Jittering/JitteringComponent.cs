using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Jittering
{
    [Friend(typeof(SharedJitteringSystem))]
    [RegisterComponent, NetworkedComponent]
    public class JitteringComponent : Component
    {
        public override string Name => "Jittering";

        [ViewVariables(VVAccess.ReadWrite)]
        public float Amplitude { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Frequency { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LastJitter { get; set; }
    }

    [Serializable, NetSerializable]
    public class JitteringComponentState : ComponentState
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
