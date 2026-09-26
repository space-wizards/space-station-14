using Content.Shared.Mind.Components;
using Robust.Shared.Timing;

namespace Content.Shared.ThoughtBubble;

/// <summary>
/// This handles thought bubble UI effects triggered by pointing at owned items.
/// </summary>

public sealed partial class SharedThoughtBubbleSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindContainerComponent, PointedOwnItemEvent>(OnPointedOwnItem);
    }

    private void OnPointedOwnItem(Entity<MindContainerComponent> ent, ref PointedOwnItemEvent args)
    {
        if (args.Handled)
            return;

        var thoughtBubble = EnsureComp<ThoughtBubbleComponent>(ent.Owner);
        thoughtBubble.PointedItem = GetNetEntity(args.Item);
        thoughtBubble.TimeEndShow = _timing.CurTime + thoughtBubble.DurationShow;
        Dirty(ent.Owner, thoughtBubble);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ThoughtBubbleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.TimeEndShow == null || _timing.CurTime < comp.TimeEndShow)
                continue;

            comp.TimeEndShow = null;
            RemCompDeferred(uid, comp);
        }
    }
}


[ByRefEvent]
public record struct PointedOwnItemEvent(EntityUid Item, bool Handled = false);
