using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ShuttleConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    public readonly ShuttleMode Mode;
    public List<(EntityUid Entity, string Destination, bool Enabled)> Destinations;

    public ShuttleConsoleBoundInterfaceState(
        ShuttleMode mode,
        List<(EntityUid Entity, string Destination, bool Enabled)> destinations,
        float maxRange,
        EntityCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks) : base(maxRange, coordinates, angle, docks)
    {
        Destinations = destinations;
        Mode = mode;
    }
}
