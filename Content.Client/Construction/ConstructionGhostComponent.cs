using Content.Shared.Construction.Prototypes;

namespace Content.Client.Construction
{
    [RegisterComponent]
    public sealed partial class ConstructionGhostComponent : Component
    {
        [ViewVariables] public ConstructionPrototype? Prototype { get; set; }
    }
}
