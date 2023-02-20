namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Colors layer in an eye color
/// </summary>
public sealed class EyeColoring : LayerColoring
{
    public override Color? GetNullableColor(Color? skin, Color? eyes, MarkingSet markingSet)
    {
        return eyes;
    }
}
