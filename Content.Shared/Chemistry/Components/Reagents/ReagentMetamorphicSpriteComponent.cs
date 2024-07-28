using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Components.Reagents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReagentMetamorphicSpriteComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier MetamorphicSprite { get; set; } = SpriteSpecifier.Invalid;

    [DataField, AutoNetworkedField]
    public int MetamorphicMaxFillLevels { get; set; } = 0;

    [DataField, AutoNetworkedField]
    public string? MetamorphicFillBaseName { get; set; } = null;

    [DataField, AutoNetworkedField]
    public bool MetamorphicChangeColor { get; set; } = true;
}
