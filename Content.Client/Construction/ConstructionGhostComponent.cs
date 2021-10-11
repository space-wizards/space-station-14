using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.Construction
{
    [RegisterComponent]
    public class ConstructionGhostComponent : Component
    {
        public override string Name => "ConstructionGhost";

        [ViewVariables] public ConstructionPrototype? Prototype { get; set; }
        [ViewVariables] public int GhostId { get; set; }
    }
}
