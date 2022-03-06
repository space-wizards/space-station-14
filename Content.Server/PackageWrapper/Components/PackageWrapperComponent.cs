using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.PackageWrapper
{
    [RegisterComponent]
    public class PackageWrapperComponent : Component
    {
        public override string Name => "PackageWrapper";

        [DataField("cableDroppedOnCutPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public readonly string CableDroppedOnCutPrototype = "CableHVStack1";
    }

    struct WrappingVisuals //TO DO: Make better name
    {

    }
}
