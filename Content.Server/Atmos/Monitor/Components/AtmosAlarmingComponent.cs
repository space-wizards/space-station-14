namespace Content.Server.Atmos.Monitor.Components;

[RegisterComponent]
public sealed class AtmosAlarmingComponent : Component
{
    /// <summary>
    ///     All registered receivers in this alarmer.
    /// </summary>
    public HashSet<string> RegisteredReceivers = new();

    // Somebody should do this someday. I'll leave it here as a reminder,
    // just in case.
    // public string StationAlarmMonitorFrequencyId
}
