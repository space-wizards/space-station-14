using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Stacks;

[Serializable, NetSerializable]
public sealed partial class StackAreaInsertEvent : DoAfterEvent
{
    public IReadOnlyList<NetEntity> StacksToMerge = default!;

    public StackAreaInsertEvent(IReadOnlyList<NetEntity> stacksToMerge)
    {
        StacksToMerge = stacksToMerge;
    }

    public override DoAfterEvent Clone() => this;
}
