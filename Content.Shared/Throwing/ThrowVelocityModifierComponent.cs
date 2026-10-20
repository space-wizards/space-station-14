namespace Content.Shared.Throwing;

[RegisterComponent]
public sealed partial class ThrowVelocityModifierComponent : Component
{
    [DataField]
    public float VelocityMultiplier = 1.0f;
}
