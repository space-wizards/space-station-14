namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Colors layer in an eye color
/// </summary>
public sealed partial class EyeColoring : LayerColoringType
{
    public override Color? GetCleanColor(Color? skin, Color? eyes, Color? speaker, MarkingSet markingSet)
    {
        return eyes;
    }
}
