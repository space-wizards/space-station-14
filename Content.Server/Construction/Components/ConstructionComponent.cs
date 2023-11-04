using Content.Shared.Construction.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Components
{
    [RegisterComponent, Access(typeof(ConstructionSystem))]
    public sealed partial class ConstructionComponent : Component
    {
        [DataField("graph", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<ConstructionGraphPrototype>))]
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
        // TODO Force flush interaction queue before serializing to YAML.
        // Otherwise you can end up with entities stuck in invalid states (e.g., waiting for DoAfters).
        public readonly Queue<object> InteractionQueue = new();
    }
}
