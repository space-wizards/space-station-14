namespace Content.Client.Botany.Components;

/// <summary>
/// Configuration for scaling a sprite by its potency value.
/// </summary>
[RegisterComponent]
public sealed partial class PotencyVisualsComponent : Component
{
    /// <summary>
    /// Minimum scale applied to the sprite.
    /// </summary>
    [DataField]
    public float MinimumScale = 1f;

    /// <summary>
    /// Maximum scale applied to the sprite.
    /// </summary>
    [DataField]
    public float MaximumScale = 2f;
}
