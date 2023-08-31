using Content.Shared.Mind.Components;
using Content.Shared.SSDIndicator;

namespace Content.Server.SSDIndicator;

/// <summary>
///     Handle changing player SSD indicator status
/// </summary>
public sealed class SSDIndicatorSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SSDIndicatorComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SSDIndicatorComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindAdded(EntityUid uid, SSDIndicatorComponent component, MindAddedMessage args)
    {
        component.IsSSD = false;
        Dirty(component);
    }

    private void OnMindRemoved(EntityUid uid, SSDIndicatorComponent component, MindRemovedMessage args)
    {
        component.IsSSD = true;
        Dirty(component);
    }
}
