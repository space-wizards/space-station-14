using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    [RegisterComponent]
    public class ExaminableSolutionComponent: Component
    {
        public override string Name => "ExaminableSolution";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";
    }
}
