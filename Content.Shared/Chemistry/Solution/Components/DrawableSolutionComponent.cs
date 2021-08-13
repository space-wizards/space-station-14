using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry.Solution.Components
{
    [RegisterComponent]
    public class DrawableSolutionComponent : Component
    {
        public override string Name => "DrawableSolution";

        [ViewVariables]
        [DataField("solution")]
        public string Solution { get; set; } = default!;
    }
}
