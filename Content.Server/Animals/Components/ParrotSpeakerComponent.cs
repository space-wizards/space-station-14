namespace Content.Server.Animals.Components;

/// <summary>
/// Makes an entity say things from parrot memory at random intervals
/// </summary>
[RegisterComponent]
public sealed partial class ParrotSpeakerComponent : Component
{
    /// <summary>
    /// Minimum time to wait after speaking to speak again
    /// </summary>
    [DataField]
    public TimeSpan MinSpeakInterval = TimeSpan.FromSeconds(2.0f * 60f);

    /// <summary>
    /// Maximum time to wait after speaking to speak again
    /// </summary>
    [DataField]
    public TimeSpan MaxSpeakInterval = TimeSpan.FromSeconds(6.0f * 60f);

    /// <summary>
    /// Next time at which the parrot speaks
    /// </summary>
    [DataField]
    public TimeSpan NextSpeakInterval = TimeSpan.FromSeconds(0.0f);
}
