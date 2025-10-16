using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedConstructionSystem))]
    public sealed partial class ConstructionComponent : Component
    {
        [DataField(required:true, customTypeSerializer:typeof(PrototypeIdSerializer<ConstructionGraphPrototype>)), AutoNetworkedField]
        public string Graph { get; set; } = string.Empty;

        [DataField(required:true), AutoNetworkedField]
        public string Node { get; set; } = default!;

        [DataField("edge")]
        public int? EdgeIndex { get; set; } = null;

        [DataField("step")]
        public int StepIndex { get; set; } = 0;

        [DataField]
        public HashSet<string> Containers { get; set; } = new();

        [DataField, AutoNetworkedField]
        public string? TargetNode { get; set; } = null;

        [ViewVariables, AutoNetworkedField]
        public int? TargetEdgeIndex { get; set; } = null;

        [ViewVariables, AutoNetworkedField]
        public Queue<string>? NodePathfinding { get; set; } = null;

        [DataField("deconstructionTarget")]
        public string? DeconstructionNode { get; set; } = "start";

        [ViewVariables]
        // TODO Force flush interaction queue before serializing to YAML.
        // Otherwise you can end up with entities stuck in invalid states (e.g., waiting for DoAfters).
        public readonly Queue<object> InteractionQueue = new();
    }
}
