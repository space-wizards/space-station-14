using Content.Shared.Construction;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class ConstructionGhostComponent : Component, IExamine
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "ConstructionGhost";

        [ViewVariables] public ConstructionPrototype? Prototype { get; set; }
        [ViewVariables] public int GhostId { get; set; }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (Prototype == null) return;

            message.AddMarkup(Loc.GetString("Building: [color=cyan]{0}[/color]\n", Prototype.Name));

            if (!_prototypeManager.TryIndex(Prototype.Graph, out ConstructionGraphPrototype? graph)) return;
            var startNode = graph.Nodes[Prototype.StartNode];

            if (!graph.TryPath(Prototype.StartNode, Prototype.TargetNode, out var path) ||
                !startNode.TryGetEdge(path[0].Name, out var edge))
            {
                return;
            }

            edge.Steps[0].DoExamine(message, inDetailsRange);
        }
    }
}
