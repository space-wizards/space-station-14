namespace Content.Server.Vocalization.Components;

/// <summary>
/// Used on animals that vocalize randomly
/// </summary>
[RegisterComponent]
public sealed partial class AnimalVocalizerComponent : Component
{
    /// <summary>
    /// Minimum length of the random string used to generate a random-length string for vocalization
    /// </summary>
    [DataField]
    public int MinRandomStringLength = 2;

    /// <summary>
    /// Maximum length of the random string used to generate a random-length string for vocalization
    /// </summary>
    [DataField]
    public int MaxRandomStringLength = 8;
}
