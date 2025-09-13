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
    public (TimeSpan Min, TimeSpan Max) TimeBetweenIncidents = (TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));

    /// <summary>
    /// The minimum duration that the entity will be unable to wake.
    /// </summary>
    [DataField]
    public (TimeSpan Min, TimeSpan Max) IncidentDuration = (TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

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
