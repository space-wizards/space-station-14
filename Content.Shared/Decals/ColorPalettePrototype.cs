using Robust.Shared.Prototypes;

namespace Content.Shared.Decals;

[Prototype("palette")]
public readonly record struct ColorPalettePrototype : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = null!;
    [DataField("name")] public string Name { get; } = null!;
    [DataField("colors")] public Dictionary<string, Color> Colors { get; } = null!;
}
