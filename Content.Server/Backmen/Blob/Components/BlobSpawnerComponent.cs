using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Backmen.Blob.Components;

[RegisterComponent]
public sealed partial class BlobSpawnerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite),
     DataField("corePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CoreBlobPrototype = "CoreBlobTile";
}
