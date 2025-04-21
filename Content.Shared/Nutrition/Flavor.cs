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
}

public enum FlavorType : byte
{
    Base,
    Complex
}
