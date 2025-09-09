namespace Content.Shared.Humanoid.ColoringScheme;

public sealed partial class HsvSaturationLimit : ColoringSchemeRule
{
    [DataField]
    public float Min { get; set; } = 0f;

    [DataField]
    public float Max { get; set; } = 1f;

    public override Color Clamp(Color color)
    {
        var hsv = Color.ToHsv(color);

        hsv.Y = Math.Clamp(hsv.Y, Min, Max);

        return Color.FromHsv(hsv);
    }
}
