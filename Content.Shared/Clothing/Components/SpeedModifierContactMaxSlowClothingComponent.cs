using Content.Shared.Clothing.EntitySystems;

namespace Content.Shared.Clothing.Components;

[RegisterComponent]
public sealed partial class SpeedModifierContactMaxSlowClothingComponent : Component
{
    public float MaxContactSprintSlowdown = 0.8f;

    public float MaxContactWalkSlowdown = 1f;
}
