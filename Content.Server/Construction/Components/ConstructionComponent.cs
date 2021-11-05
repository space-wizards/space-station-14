using System.Collections.Generic;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Construction.Components
{
    [RegisterComponent, Friend(typeof(ConstructionSystem))]
    public class ConstructionComponent : Component
    {
        public override string Name => "Construction";

        [DataField("graph", required:true)]
        public string Graph { get; set; } = string.Empty;

        [DataField("node", required:true)]
        public string Node { get; set; } = default!;

        [DataField("edge")]
        public int? EdgeIndex { get; set; } = null;

        [DataField("step")]
        public int StepIndex { get; set; } = 0;

        [DataField("containers")]
        public HashSet<string> Containers { get; set; } = new();

        [DataField("defaultTarget")]
        public string? TargetNode { get; set; } = null;

        [ViewVariables]
        public int? TargetEdgeIndex { get; set; } = null;

        [ViewVariables]
        public Queue<string>? NodePathfinding { get; set; } = null;

        [DataField("deconstructionTarget")]
        public string? DeconstructionNode { get; set; } = "start";

        [ViewVariables]
        public bool WaitingDoAfter { get; set; } = false;

        [ViewVariables]
        public readonly Queue<object> InteractionQueue = new();
    }
}
