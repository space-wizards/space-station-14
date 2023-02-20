namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Colors layer in a specified color
/// </summary>
public sealed class SimpleColoring : LayerColoring
{
    [DataField("color", required: true)]
    public Color Color = Color.White;

    public override Color? GetNullableColor(Color? skin, Color? eyes, MarkingSet markingSet)
    {
        return Color;
    }
}
