using System.Collections.Concurrent;
using System.Threading.Tasks;
using Content.Server._NullLink.Core;
using Content.Server._NullLink.Helpers;
using Robust.Shared.Timing;
using Starlight.NullLink;
using Starlight.NullLink.Event;

namespace Content.Server._NullLink.EventBus;

public sealed partial class EventBusSystem : EntitySystem
{
    private const int MaxEventsPerTick = 3;

    [Dependency] private readonly INullLinkEventBusManager _nullLinkEventBusManager = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        for (var i = 0; i < MaxEventsPerTick && _nullLinkEventBusManager.TryDequeue(out var @event); i++)
            RaiseLocalEvent(@event);
    }
}