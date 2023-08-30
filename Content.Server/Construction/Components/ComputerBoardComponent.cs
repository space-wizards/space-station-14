using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Components
{
    /// <summary>
    /// Used for construction graphs in building computers.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ComputerBoardComponent : Component
    {
        [DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype { get; private set; }
    }
}
