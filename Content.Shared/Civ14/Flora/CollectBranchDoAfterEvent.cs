using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Flora;

[Serializable, NetSerializable]
public sealed partial class CollectBranchDoAfterEvent : SimpleDoAfterEvent
{
}