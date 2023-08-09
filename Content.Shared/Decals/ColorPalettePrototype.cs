using Robust.Shared.Prototypes;

namespace Content.Shared.Decals;

[Prototype("palette")]
public sealed class ColorPalettePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = null!;
    [DataField("name")] public string Name { get; } = null!;
    [DataField("colors")] public Dictionary<string, Color> Colors { get; } = null!;
}
