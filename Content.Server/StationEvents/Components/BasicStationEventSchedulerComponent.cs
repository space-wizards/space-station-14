namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BasicStationEventSchedulerSystem))]
public sealed partial class BasicStationEventSchedulerComponent : Component
{
    /// <summary>
    ///     The minimum amount of time that must past before the first event can trigger.
    ///     This is in addition to the min and max times below.
    /// </summary>
    [DataField(), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinimumTimeUntilFirstEvent = TimeSpan.FromSeconds(300);

    /// <summary>
    ///     The minimum amount of time that must past after the last event before the next event check will occur.
    /// </summary>
    [DataField(), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinimumTimeBetweenEvents = TimeSpan.FromSeconds(300);

    /// <summary>
    ///     The maximum amount of time that must past after the last event before the next event check will occur.
    /// </summary>
    [DataField(), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaximumTimeBetweenEvents = TimeSpan.FromSeconds(600);

    /// <summary>
    ///     The time at which the next event check will occur.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextEventTime = TimeSpan.Zero;
}
