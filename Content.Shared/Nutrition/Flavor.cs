using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition;

[Prototype]
public sealed partial class FlavorPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("flavorType")]
    public FlavorType FlavorType { get; private set; } = FlavorType.Base;

    [DataField("description")]
    public string FlavorDescription { get; private set; } = default!;

    // DS14-flavor-neutralize-start
    [DataField]
    public List<ProtoId<FlavorPrototype>> Neutralize = new();
    // DS14-flavor-neutralize-end
}

public enum FlavorType : byte
{
    Base,
    Complex
}
