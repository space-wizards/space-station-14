namespace Content.Shared.Effects;

/// <summary>
/// Stores the original sprite color for flashing entity to be able to restore it later.
/// </summary>
[RegisterComponent]
public sealed partial class ColorFlashEffectComponent : Component
{
    [ViewVariables]
    public Color Color = Color.White;
}
