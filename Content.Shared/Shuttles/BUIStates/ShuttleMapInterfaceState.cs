using Content.Shared.Shuttles.Systems;
using Content.Shared.Shuttles.UI.MapObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

/// <summary>
/// Handles BUI data for Map screen.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleMapInterfaceState
{
    /// <summary>
    /// The current FTL state.
    /// </summary>
    public readonly FTLState FTLState;

    /// <summary>
    /// How long the FTL state takes.
    /// </summary>
    public float FTLDuration;

    public List<ShuttleBeaconObject> Destinations;

    public List<ShuttleExclusionObject> Exclusions;

    public ShuttleMapInterfaceState(
        FTLState ftlState,
        float ftlDuration,
        List<ShuttleBeaconObject> destinations,
        List<ShuttleExclusionObject> exclusions)
    {
        FTLState = ftlState;
        FTLDuration = ftlDuration;
        Destinations = destinations;
        Exclusions = exclusions;
    }
}
