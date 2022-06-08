using Content.Shared.Shuttles.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ShuttleConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    public readonly ShuttleMode Mode;

    public ShuttleConsoleBoundInterfaceState(ShuttleMode mode, float range, EntityUid? entity) : base(range, entity)
    {
        Mode = mode;
    }
}
