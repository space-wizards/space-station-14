using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Stains;

public abstract class SharedStainsSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed partial class SqueezeDoAfterEvent : SimpleDoAfterEvent
{
}
