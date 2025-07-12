using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Animals.Components;

/// <summary>
/// Makes an entity have a persistent memory of messages for use with a ParrotSpeakerComponent
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ParrotDbMemoryComponent : Component
{
    /// <summary>
    /// How often the persistent memory cache refreshes, loading new cross-round messages into a parrot memory
    /// </summary>
    [DataField]
    public TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The next time at which this component will refresh the memory
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextRefresh = TimeSpan.Zero;

    /// <summary>
    /// Minimum playtime requirement for a player before their messages can be committed to persistent memory
    /// </summary>
    [DataField]
    public TimeSpan MinimumSourcePlaytime = TimeSpan.FromHours(5);
}
