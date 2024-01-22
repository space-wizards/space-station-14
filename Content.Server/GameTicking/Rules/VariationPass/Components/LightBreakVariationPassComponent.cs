namespace Content.Server.GameTicking.Rules.VariationPass.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class LightBreakVariationPassComponent : Component
{
    [DataField]
    public float LightBreakChance = 0.07f;
}
