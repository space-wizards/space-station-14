using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BlobSpawnRule))]
public sealed class BlobSpawnRuleComponent : Component
{
    [DataField("carrierBlobProtos", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public List<string> CarrierBlobProtos = new()
    {
        "MobMouse",
        "MobMouse1",
        "MobMouse2"
    };

    [ViewVariables(VVAccess.ReadOnly), DataField("playersPerCarrierBlob")]
    public int PlayersPerCarrierBlob = 30;

    [ViewVariables(VVAccess.ReadOnly), DataField("maxCarrierBlob")]
    public int MaxCarrierBlob = 3;
}
