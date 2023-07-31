using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Fluids;

public class SharedDrainSystem : EntitySystem
{
    [Serializable, NetSerializable]
    public sealed class DrainDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
