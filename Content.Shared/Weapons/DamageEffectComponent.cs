namespace Content.Shared.Weapons;

/// <summary>
/// Stores the original sprite color for a damaged entity to be able to restore it later.
/// </summary>
[RegisterComponent]
public sealed class DamageEffectComponent : Component
{
    [ViewVariables]
    public Color Color = Color.White;
}
