#nullable enable
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Helpers;

/// <summary>
/// Generic system that listens for and records any received events of a given type.
/// </summary>
public abstract class TestListenerSystem<TEvent> : EntitySystem where TEvent : notnull
{
    public override void Initialize()
    {
        // TODO
        // supporting broadcast events requires cleanup on test finish, which will probably require  changes to the
        // test pair/pool manager and would conflict with #36797
        SubscribeLocalEvent<TestListenerComponent, TEvent>(OnDirectedEvent);
    }

    protected virtual void OnDirectedEvent(Entity<TestListenerComponent> ent, ref TEvent args)
    {
        ent.Comp.Events.GetOrNew(args.GetType()).Add(args);
    }

    public int Count(EntityUid uid, Func<TEvent, bool>? predicate = null)
    {
        return GetEvents(uid, predicate).Count();
    }

    public void Clear(EntityUid uid)
    {
        CompOrNull<TestListenerComponent>(uid)?.Events.Remove(typeof(TEvent));
    }

    public IEnumerable<TEvent> GetEvents(EntityUid uid, Func<TEvent, bool>? predicate = null)
    {
        var events = CompOrNull<TestListenerComponent>(uid)?.Events.GetValueOrDefault(typeof(TEvent));
        if (events == null)
            return [];

        return events.Cast<TEvent>().Where(e => predicate?.Invoke(e) ?? true);
    }
}
