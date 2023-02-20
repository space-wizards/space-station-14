namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Colors layer in a skin color
/// </summary>
public sealed class SkinColoring : LayerColoring
{
    public override Color? GetNullableColor(Color? skin, Color? eyes, MarkingSet markingSet)
    {
        return skin;
    }
}
