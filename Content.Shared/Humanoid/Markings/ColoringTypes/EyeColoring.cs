namespace Content.Shared.Humanoid.Markings.ColoringTypes;

/// <summary>
///     Colors layer in an eye color
/// </summary>
public sealed partial class EyeColoring : LayerColoringType
{
    public override Color? GetCleanColor(Color? skin, Color? eyes, List<Marking> otherMarkings)
    {
        return eyes;
    }
}
