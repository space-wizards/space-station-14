using Content.Shared.Disposal.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal;

[Serializable, NetSerializable]
public sealed class MailingUnitBoundUserInterfaceState: BoundUserInterfaceState, IEquatable<MailingUnitBoundUserInterfaceState>
{
    public string? Target;
    public List<string> TargetList;
    public string? Tag;
    public SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState DisposalState;

    public MailingUnitBoundUserInterfaceState(SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState disposalState, string? target, List<string> targetList, string? tag)
    {
        DisposalState = disposalState;
        Target = target;
        TargetList = targetList;
        Tag = tag;
    }

    public bool Equals(MailingUnitBoundUserInterfaceState? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return DisposalState.Equals(other.DisposalState)
               && Target == other.Target
               && TargetList.Equals(other.TargetList)
               && Tag == other.Tag;
    }
}
