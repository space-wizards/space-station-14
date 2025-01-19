using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Starlight.Antags.Abductor;

[Prototype("abductorListing")]
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class AbductorListingPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
    
    [DataField(required: true)]
    string Name;
    
    [DataField(required: true)]
    int Cost;
}