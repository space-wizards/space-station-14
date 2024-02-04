using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ShuttleConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    /// <summary>
    /// The current FTL state.
    /// </summary>
    public readonly FTLState FTLState;

    /// <summary>
    ///  When the next FTL state change happens.
    /// </summary>
    public readonly TimeSpan FTLTime;

    public List<ShuttleDestination> Destinations;

    public List<ShuttleExclusion> Exclusions;

    public ShuttleConsoleBoundInterfaceState(
        FTLState ftlState,
        TimeSpan ftlTime,
        List<ShuttleDestination> destinations,
        List<ShuttleExclusion> exclusions,
        float maxRange,
        NetCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks) : base(maxRange, coordinates, angle, docks)
    {
        FTLState = ftlState;
        FTLTime = ftlTime;
        Destinations = destinations;
        Exclusions = exclusions;
    }
}

[Serializable, NetSerializable]
public readonly record struct ShuttleDestination(NetCoordinates Entity, string Destination, bool Enabled);

[Serializable, NetSerializable]
public record struct ShuttleExclusion(NetCoordinates Coordinates, float Range);
