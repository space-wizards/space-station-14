using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Singularity.Components
{
    [RegisterComponent]
    public class SingularityDistortionComponent : Component
    {
        public override string Name => "SingularityDistortion";

        [DataField("intensity")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Intensity { get; set; } = 2;

        [DataField("falloff")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Falloff { get; set; } = 7;
    }
}
