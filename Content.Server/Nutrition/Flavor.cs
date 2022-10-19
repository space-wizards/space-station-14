using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition;

[Prototype("flavor")]
public readonly record struct FlavorPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("flavorType")]
    public FlavorType FlavorType { get; } = FlavorType.Base;

    [DataField("description")]
    public string FlavorDescription { get; } = default!;
}

public enum FlavorType : byte
{
    Base,
    Complex
}
