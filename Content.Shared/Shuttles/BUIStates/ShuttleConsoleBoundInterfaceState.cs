using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ShuttleConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    public readonly FTLState FTLState;
    public readonly float FTLAccumulator;
    public readonly ShuttleMode Mode;
    public List<(EntityUid Entity, string Destination, bool Enabled)> Destinations;

    public ShuttleConsoleBoundInterfaceState(
        FTLState ftlState,
        float ftlAccumulator,
        ShuttleMode mode,
        List<(EntityUid Entity, string Destination, bool Enabled)> destinations,
        float maxRange,
        EntityCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks) : base(maxRange, coordinates, angle, docks)
    {
        FTLState = ftlState;
        FTLAccumulator = ftlAccumulator;
        Destinations = destinations;
        Mode = mode;
    }
}
