using Content.Server.Disposal.Unit.Components;

namespace Content.Server.Disposal.Tube;

[ByRefEvent]
public record struct GetDisposalsConnectableDirectionsEvent
{
    public Direction[] Connectable;
}
