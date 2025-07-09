using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Vocalization.Components;

/// <summary>
/// Makes an entity vocalize at set intervals
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class VocalizerComponent : Component
{
    /// <summary>
    /// Minimum time to wait after speaking to vocalize again
    /// </summary>
    [DataField]
    public TimeSpan MinVocalizeInterval = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Maximum time to wait after speaking to vocalize again
    /// </summary>
    [DataField]
    public TimeSpan MaxVocalizeInterval = TimeSpan.FromMinutes(6);

    /// <summary>
    /// Next time at which to vocalize
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextVocalizeInterval = TimeSpan.Zero;
}
