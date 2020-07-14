using Content.Shared.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class ConstructionGhostComponent : Component
    {
        public override string Name => "ConstructionGhost";

        [ViewVariables] public ConstructionPrototype Prototype { get; set; }
        [ViewVariables] public int GhostID { get; set; }
    }
}
