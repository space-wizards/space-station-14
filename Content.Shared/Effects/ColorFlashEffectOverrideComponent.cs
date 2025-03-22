namespace Content.Shared.Effects;

[RegisterComponent]
public sealed partial class ColorFlashEffectOverrideComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EffectSource, Color> Values = new ();
}
