using Content.Shared.Mind.Components;
using Content.Shared.NPC;

namespace Content.Shared.SSDIndicator;

/// <summary>
///     Handle changing player SSD indicator status
/// </summary>
public sealed class SSDIndicatorSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SSDIndicatorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SSDIndicatorComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SSDIndicatorComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnInit(EntityUid uid, SSDIndicatorComponent component, ComponentInit args)
    {
        if (HasComp<ActiveNPCComponent>(uid))
            return;

        component.IsSSD = !HasComp<MindContainerComponent>(uid);
    }

    private void OnMindAdded(EntityUid uid, SSDIndicatorComponent component, MindAddedMessage args)
    {
        component.IsSSD = false;
        Dirty(uid, component);
    }

    private void OnMindRemoved(EntityUid uid, SSDIndicatorComponent component, MindRemovedMessage args)
    {
        if (HasComp<ActiveNPCComponent>(uid))
            return;

        component.IsSSD = true;
        Dirty(uid, component);
    }
}
