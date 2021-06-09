using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Singularity
{

    [RegisterComponent]
    [ComponentReference(typeof(IClientSingularityInstance))]
    public class ToySingularityComponent : Component, IClientSingularityInstance
    {
        public override string Name => "ToySingularity";
        [ViewVariables(VVAccess.ReadWrite)]
        public float Falloff { get; set; } = 2.0f;
        [ViewVariables(VVAccess.ReadWrite)]
        public float Intensity { get; set; } = 0.25f;
    }
}
