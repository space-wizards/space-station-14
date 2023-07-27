using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Blob
{
    [RegisterComponent]
    public sealed class BlobSpawnerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite),
         DataField("corePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string CoreBlobPrototype = "CoreBlobTile";
    }
}
