using Content.Server.Kitchen.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Kitchen.Components
{
    /// <summary>
    /// Tag component that denotes an entity as Extractable
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(ReagentGrinderSystem))]
    public class ExtractableComponent : Component
    {
        [ViewVariables]
        [DataField("juiceSolution")]
        public Solution? JuiceSolution;

        [ViewVariables]
        [DataField("grindableSolutionName")]
        public string? GrindableSolution;
    }
}
