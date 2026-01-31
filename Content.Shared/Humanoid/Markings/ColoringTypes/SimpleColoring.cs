namespace Content.Shared.Humanoid.Markings.ColoringTypes;

/// <summary>
///     Colors layer in a specified color
/// </summary>
public sealed partial class SimpleColoring : LayerColoringType
{
    [DataField("color", required: true)]
    public Color Color = Color.White;

    public override Color? GetCleanColor(Color? skin, Color? eyes, List<Marking> otherMarkings)
    {
        return Color;
    }
}
