namespace Content.Server.GameTicking.Rules.VariationPass.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class LightBreakVariationPassComponent : Component
{
    [DataField]
    public float LightBreakChanceAverage = 0.07f;

    [DataField]
    public float LightBreakChanceStdDev = 0.01f;
}
