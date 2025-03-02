using Content.Shared.Disposal.Components;
using DisposalEntryComponent = Content.Shared.Disposal.Tube.Components.DisposalEntryComponent;

namespace Content.Shared.Disposal.Unit;

public abstract class SharedDisposalTubeSystem : EntitySystem
{
    public virtual bool TryInsert(EntityUid uid,
        DisposalUnitComponent from,
        IEnumerable<string>? tags = default,
        DisposalEntryComponent? entry = null)
    {
        return false;
    }
}
