using Content.Shared.Starlight.Antags.Abductor;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Computers.RemoteEye;

[Serializable, NetSerializable]
public sealed class RemoteEyeConsoleBuiState : BoundUserInterfaceState
{
    public required Dictionary<int, StationBeacons> Stations { get; init; }
    public required Color Color { get; init; }
}
