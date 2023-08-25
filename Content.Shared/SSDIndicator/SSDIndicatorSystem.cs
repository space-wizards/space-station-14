using Content.Shared.Mind.Components;

namespace Content.Shared.SSDIndicator;

/// <summary>
///     Handle changing player SSD indicator status
/// </summary>
public sealed class SSDIndicatorSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SSDIndicatorComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SSDIndicatorComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindAdded(EntityUid uid, SSDIndicatorComponent component, MindAddedMessage args)
    {
        component.IsSSD = false;
        Dirty(uid, component);
    }

    private void OnMindRemoved(EntityUid uid, SSDIndicatorComponent component, MindRemovedMessage args)
    {
        component.IsSSD = true;
        Dirty(uid, component);
    }
}
