namespace Content.Shared.Effects;

/// <summary>
/// When a ColorFlashEffect is applied to this entity, the color can be overridden for individual effect sources.
/// For example, an entity can have all "HitDamage" ColorFlash effects transformed to green.
/// </summary>
[RegisterComponent]
public sealed partial class ColorFlashEffectOverrideComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, Color> Values = new ();
}
