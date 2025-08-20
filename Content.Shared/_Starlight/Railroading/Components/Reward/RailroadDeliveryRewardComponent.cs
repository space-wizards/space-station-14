using System;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadDeliveryRewardComponent : Component
{
    [DataField("delivery", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Delivery;
    
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Dataset = null;
    
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? WrappedDataset = null;
}
