namespace Content.Shared.Throwing;

/// <summary>
/// Applies veolicty multiplier on thrown item.
/// </summary>
[RegisterComponent]
public sealed partial class ThrowVelocityModifierComponent : Component
{
    /// <summary>
    /// Veolicty multiplier applied to the entity when thrown
    /// </summary>
    [DataField]
    public float VelocityMultiplier = 1.0f;
}
