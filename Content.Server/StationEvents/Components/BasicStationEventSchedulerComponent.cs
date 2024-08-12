using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BasicStationEventSchedulerSystem))]
public sealed partial class BasicStationEventSchedulerComponent : Component
{
    public const float MinimumTimeUntilFirstEvent = 300;

    /// <summary>
    /// How long until the next check for an event runs
    /// </summary>
    /// Default value is how long until first event is allowed
    [ViewVariables(VVAccess.ReadWrite)]
    public float TimeUntilNextEvent = MinimumTimeUntilFirstEvent;

    /// <summary>
    /// The gamerules that the scheduler can choose from
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;
}
