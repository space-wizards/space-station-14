using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Components.Reagents;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReagentMetamorphicSpriteComponent : Component
{
    [DataField]
    public SpriteSpecifier MetamorphicSprite { get; set; } = SpriteSpecifier.Invalid;

    [DataField]
    public int MetamorphicMaxFillLevels { get; set; } = 0;

    [DataField]
    public string? MetamorphicFillBaseName { get; set; } = null;

    [DataField]
    public bool MetamorphicChangeColor { get; set; } = true;
}
