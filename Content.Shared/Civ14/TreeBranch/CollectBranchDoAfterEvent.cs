using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.TreeBranch;

[Serializable, NetSerializable]
public sealed partial class CollectBranchDoAfterEvent : SimpleDoAfterEvent
{
}