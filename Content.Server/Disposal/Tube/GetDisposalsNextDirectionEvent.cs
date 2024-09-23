using DisposalHolderComponent = Content.Server.Disposal.Unit.DisposalHolderComponent;

namespace Content.Server.Disposal.Tube;

[ByRefEvent]
public record struct GetDisposalsNextDirectionEvent(DisposalHolderComponent Holder)
{
    public Direction Next;
}
