using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed class OxygenCylinderComponent : Component
    {
        public override string Name => "OxygenCylinder";

        [DataField("maxOxygen")]
        public float MaxOxygen { get; set; } = 100f;

        [DataField("currentOxygen")]
        public float CurrentOxygen { get; set; } = 100f;

        [DataField("oxygenUseRate")]
        public float OxygenUseRate { get; set; } = 0.1f;
    }
}
