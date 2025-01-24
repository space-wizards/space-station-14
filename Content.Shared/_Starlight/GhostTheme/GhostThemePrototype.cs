using Content.Shared.Starlight.Utility;
using Content.Shared.Starlight;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared.Starlight.GhostTheme;

[Prototype("ghostTheme")]
public sealed class GhostThemePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;
    
    [DataField("name")]
    public string Name { get; private set; } = string.Empty;
    
    [DataField("description")]
    public string Description { get; private set; } = string.Empty;
    
    [DataField("spriteSpecifier", required: true)]
    public ExtendedSpriteSpecifier SpriteSpecifier { get; private set; } = default!;
    
    [DataField("requiredFlags", required: true)]
    public List<PlayerFlags> Flags = [];
    
    [DataField("requiredCkey")]
    public string? Ckey = null;
}