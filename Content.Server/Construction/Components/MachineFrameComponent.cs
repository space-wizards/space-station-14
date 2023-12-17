using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Construction.Components
{
    [RegisterComponent]
    public sealed partial class MachineFrameComponent : Component
    {
        public const string PartContainerName = "machine_parts";
        public const string BoardContainerName = "machine_board";

        [ViewVariables]
        public bool HasBoard => BoardContainer?.ContainedEntities.Count != 0;

        [DataField("progress", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, MachinePartPrototype>))]
        public Dictionary<string, int> Progress = new();

        [ViewVariables]
        public readonly Dictionary<string, int> MaterialProgress = new();

        [ViewVariables]
        public readonly Dictionary<string, int> ComponentProgress = new();

        [ViewVariables]
        public readonly Dictionary<string, int> TagProgress = new();

        [DataField("requirements", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, MachinePartPrototype>))]
        public Dictionary<string, int> Requirements = new();

        [ViewVariables]
        public Dictionary<string, int> MaterialRequirements = new();

        [ViewVariables]
        public Dictionary<string, GenericPartInfo> ComponentRequirements = new();

        [ViewVariables]
        public Dictionary<string, GenericPartInfo> TagRequirements = new();

        [ViewVariables]
        public Container BoardContainer = default!;

        [ViewVariables]
        public Container PartContainer = default!;
    }
}
