using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Cargo.Prototypes;

[Prototype("cargoShuttle")]
public sealed class CargoShuttlePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [ViewVariables, DataField("path")]
    public ResourcePath Path = default!;

    [ViewVariables, DataField("nameDataset", customTypeSerializer:typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string NameDataset = "CargoShuttleNames";
}
