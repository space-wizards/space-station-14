namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Colors layer in skin color but much darker.
/// </summary>
public sealed class TattooColoring : LayerColoring
{
    public override Color? GetNullableColor(Color? skin, Color? eyes, MarkingSet markingSet)
    {
        if (skin == null) return null;

        var newColor = Color.ToHsv(skin.Value);
        newColor.Z = .20f;

        return Color.FromHsv(newColor);
    }
}
