using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Animals.Components;

/// <summary>
/// Makes an entity say things from parrot memory at random intervals
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ParrotSpeakerComponent : Component
{
    /// <summary>
    /// Minimum time to wait after speaking to speak again
    /// </summary>
    [DataField]
    public TimeSpan MinSpeakInterval = TimeSpan.FromMinutes(2.0f);

    /// <summary>
    /// Maximum time to wait after speaking to speak again
    /// </summary>
    [DataField]
    public TimeSpan MaxSpeakInterval = TimeSpan.FromMinutes(6.0f);

    /// <summary>
    /// Next time at which the parrot speaks
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextSpeakInterval = TimeSpan.FromSeconds(0.0f);
}
