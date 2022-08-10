using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.Construction;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Server.Construction.Components
{
    [RegisterComponent]
    public sealed class MachineFrameComponent : Component
    {
        public const string PartContainerName = "machine_parts";
        public const string BoardContainerName = "machine_board";

        [ViewVariables]
        public bool HasBoard => BoardContainer?.ContainedEntities.Count != 0;

        [ViewVariables]
        public readonly Dictionary<MachinePart, int> Progress = new();

        [ViewVariables]
        public readonly Dictionary<string, int> MaterialProgress = new();

        [ViewVariables]
        public readonly Dictionary<string, int> ComponentProgress = new();

        [ViewVariables]
        public readonly Dictionary<string, int> TagProgress = new();

        [ViewVariables]
        public Dictionary<MachinePart, int> Requirements = new();

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

    [DataDefinition]
    public sealed class MachineDeconstructedEvent : EntityEventArgs
    {
    }
}
