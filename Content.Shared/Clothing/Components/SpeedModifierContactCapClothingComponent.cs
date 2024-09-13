using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// When equipped, sets a max cap to the slowdown applied from contact speed modifiers. (E.g. glue puddles, kudzu).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SpeedModifierContactCapClothingSystem))]
public sealed partial class SpeedModifierContactCapClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MaxContactSprintSlowdown = 1f;

    [DataField, AutoNetworkedField]
    public float MaxContactWalkSlowdown = 1f;
}
