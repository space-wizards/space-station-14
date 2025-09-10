using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.Construction
{
    [RegisterComponent]
    public sealed partial class ConstructionGhostComponent : Component
    {
        public int GhostId { get; set; }
        [ViewVariables] public ConstructionPrototype? Prototype { get; set; }
    }
}
