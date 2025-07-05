using System.Numerics;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This is used for the narcolepsy trait.
/// </summary>
[RegisterComponent, Access(typeof(NarcolepsySystem))]
[AutoGenerateComponentPause]
public sealed partial class NarcolepsyComponent : Component
{
    /// <summary>
    /// The minimum time between naps.
    /// </summary>
    [DataField]
    public TimeSpan MinTimeBetweenIncidents = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The maximum time between naps.
    /// </summary>
    [DataField]
    public TimeSpan MaxTimeBetweenIncidents = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The minimum duration that the entity will be unable to wake.
    /// </summary>
    [DataField]
    public TimeSpan MinIncidentDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The maximum duration the entity will be unable to wake.
    /// </summary>
    [DataField]
    public TimeSpan MaxIncidentDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The next time a forced sleep will occur.
    /// </summary>
    [AutoPausedField]
    public TimeSpan NextIncidentTime;

    /// <summary>
    /// Whether the entity is asleep due to narcolepsy. (As opposed to normal sleep or chemically induced sleep.)
    /// </summary>
    public bool NarcolepsyInducedSleep;
}
