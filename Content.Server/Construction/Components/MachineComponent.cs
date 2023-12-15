using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Components
{
    [RegisterComponent, ComponentProtoName("Machine")]
    public sealed partial class MachineComponent : Component
    {
        [DataField("board", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? BoardPrototype { get; private set; }

        [ViewVariables]
        public Container BoardContainer = default!;
        [ViewVariables]
        public Container PartContainer = default!;
    }

    /// <summary>
    /// The different types of scaling that are available for machine upgrades
    /// </summary>
    public enum MachineUpgradeScalingType : byte
    {
        Linear,
        Exponential
    }
}
