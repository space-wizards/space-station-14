using Content.Server.Pressure;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public class PressureProtectionComponent : Component, IPressureProtection
    {
        public override string Name => "PressureProtection";

        [ViewVariables]
        [DataField("highPressureMultiplier")]
        public float HighPressureMultiplier { get; private set; } = 1f;

        [ViewVariables]
        [DataField("lowPressureMultiplier")]
        public float LowPressureMultiplier { get; private set; } = 1f;
    }
}
