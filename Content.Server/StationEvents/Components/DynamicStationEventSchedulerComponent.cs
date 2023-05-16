namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(DynamicStationEventSchedulerSystem))]
public sealed class DynamicStationEventSchedulerComponent : Component
{
    public const float MinimumTimeUntilFirstEvent = 300;

    /// <summary>
    /// How long until the next check for an event runs
    /// </summary>
    /// Default value is how long until first event is allowed
    [ViewVariables(VVAccess.ReadWrite)]
    public float TimeUntilNextEvent = MinimumTimeUntilFirstEvent;
}
