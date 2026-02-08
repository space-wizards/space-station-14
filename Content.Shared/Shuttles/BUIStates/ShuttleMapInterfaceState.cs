using Content.Shared.Shuttles.Systems;
using Content.Shared.Shuttles.UI.MapObjects;
using Content.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

/// <summary>
/// Handles BUI data for Map screen.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleMapInterfaceState(
    FTLState ftlState,
    StartEndTime ftlTime,
    List<ShuttleBeaconObject> destinations,
    List<ShuttleExclusionObject> exclusions)
{
    /// <summary>
    /// The current FTL state.
    /// </summary>
    public readonly FTLState FTLState = ftlState;

    /// <summary>
    /// When the current FTL state starts and ends.
    /// </summary>
    public StartEndTime FTLTime = ftlTime;

    public List<ShuttleBeaconObject> Destinations = destinations;

    public List<ShuttleExclusionObject> Exclusions = exclusions;
}
