namespace Content.Shared.Humanoid;
/// <summary>
/// Raised on an entity when their HumanoidAppearanceComponent changes
/// </summary>
[ByRefEvent]
public record struct LoadedHumanoidAppearanceEvent()
{
    /// <summary>
    /// The entity getting the appearance component.
    /// </summary>
    public EntityUid Owner;

    /// <summary>
    /// The apearance component that was modified.
    /// </summary>
    public HumanoidAppearanceComponent? AppearanceComp;
}
