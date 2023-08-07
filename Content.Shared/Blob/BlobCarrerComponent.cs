using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Blob;

[RegisterComponent, NetworkedComponent]
public sealed class BlobCarrierComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("transformationDelay")]
    public float TransformationDelay = 240;

    [ViewVariables(VVAccess.ReadWrite), DataField("alertInterval")]
    public float AlertInterval = 30f;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextAlert = TimeSpan.FromSeconds(0);

    [ViewVariables(VVAccess.ReadWrite)]
    public bool HasMind = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TransformationTimer = 0;

    [ViewVariables(VVAccess.ReadWrite),
     DataField("corePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CoreBlobPrototype = "CoreBlobTile";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("coreBlobGhostRolePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CoreBlobGhostRolePrototype = "CoreBlobTileGhostRole";
}

public sealed class TransformToBlobActionEvent : InstantActionEvent
{

}

