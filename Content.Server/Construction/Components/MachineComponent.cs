using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Components
{
    [RegisterComponent, ComponentProtoName("Machine")]
    public sealed class MachineComponent : Component
    {
        [DataField("board")]
        public string? BoardPrototype { get; private set; }

        public Container BoardContainer = default!;
        public Container PartContainer = default!;
    }
}
