using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpeedModifierContactMaxSlowClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MaxContactSprintSlowdown = 1f;

    [DataField, AutoNetworkedField]
    public float MaxContactWalkSlowdown = 1f;
}
