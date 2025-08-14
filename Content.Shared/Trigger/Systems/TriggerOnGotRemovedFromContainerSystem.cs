using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Containers;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// System for creating a trigger when an entity gets removed from a container.
/// </summary>
public sealed class TriggerOnGotRemovedFromContainer : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnGotRemovedFromContainerComponent, EntGotRemovedFromContainerMessage>(OnGotRemovedFromContainer);
    }

    private void OnGotRemovedFromContainer(Entity<TriggerOnGotRemovedFromContainerComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        _trigger.Trigger(ent.Owner, args.Entity, ent.Comp.KeyOut);
    }
}
