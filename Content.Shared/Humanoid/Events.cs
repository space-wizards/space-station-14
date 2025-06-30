namespace Content.Shared.Humanoid;
/// <summary>
/// Raised on an entity when their HumanoidAppearanceComponent changes.
/// Note that this will run multiple times
/// when that may not be the expected behaviour.
/// Once when first loading in, to load a default profile -
/// then again to load a custom profile.
/// </summary>
[ByRefEvent]
public record struct LoadedHumanoidAppearanceEvent()
{
}
