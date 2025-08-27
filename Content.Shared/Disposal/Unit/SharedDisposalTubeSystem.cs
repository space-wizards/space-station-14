using Content.Shared.Disposal.Components;

namespace Content.Shared.Disposal.Unit;

public abstract class SharedDisposalTubeSystem : EntitySystem
{
    public virtual bool TryInsert(EntityUid uid,
        DisposalUnitComponent from,
        IEnumerable<string>? tags = default,
        Tube.DisposalEntryComponent? entry = null)
    {
        return false;
    }
}
