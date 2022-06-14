using Content.Server.Kitchen.EntitySystems;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Kitchen.Components
{
    /// <summary>
    /// Tag component that denotes an entity as Extractable
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ReagentGrinderSystem))]
    public sealed class ExtractableComponent : Component
    {
        [ViewVariables]
        [DataField("juiceSolution")]
        public Solution? JuiceSolution;

        [ViewVariables]
        [DataField("grindableSolutionName")]
        public string? GrindableSolution;
    }
}
