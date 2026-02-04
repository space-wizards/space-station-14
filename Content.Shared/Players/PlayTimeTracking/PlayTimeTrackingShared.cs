using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.Players.PlayTimeTracking;

public static class PlayTimeTrackingShared
{
    /// <summary>
    /// The prototype ID of the play time tracker that represents overall playtime, i.e. not tied to any one role.
    /// </summary>
    public static readonly ProtoId<PlayTimeTrackerPrototype> TrackerOverall = "Overall";

    /// <summary>
    /// The prototype ID of the play time tracker that represents admin time, when a player is in game as admin.
    /// </summary>
    public static readonly ProtoId<PlayTimeTrackerPrototype> TrackerAdmin = "Admin";
}
