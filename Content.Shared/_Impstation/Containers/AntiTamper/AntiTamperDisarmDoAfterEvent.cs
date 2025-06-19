using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Containers.AntiTamper;

[Serializable, NetSerializable]
public sealed partial class AntiTamperDisarmDoAfterEvent : SimpleDoAfterEvent
{
}
