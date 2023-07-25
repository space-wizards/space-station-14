using Robust.Shared.Serialization;

namespace Content.Shared._FTL.ShipTracker;

public abstract class SharedShipTrackerSystem : EntitySystem
{
    [Serializable, NetSerializable]
    public enum ShieldGeneratorVisuals : byte
    {
        State
    }
}
