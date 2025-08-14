using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Containers;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// System for creating triggers when entities are inserted into or removed from containers.
/// </summary>
public sealed class TriggerOnContainerInteraction : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnInsertedIntoContainerComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<TriggerOnRemovedFromContainerComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<TriggerOnGotInsertedIntoContainerComponent, EntGotInsertedIntoContainerMessage>(OnGotInsertedIntoContainer);
        SubscribeLocalEvent<TriggerOnGotRemovedFromContainerComponent, EntGotRemovedFromContainerMessage>(OnGotRemovedFromContainer);

    }

    // Used by containers to trigger when entities are inserted into or removed from them
    private void OnInsertedIntoContainer(Entity<TriggerOnInsertedIntoContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        _trigger.Trigger(ent.Owner, args.Entity, ent.Comp.KeyOut);
    }

    private void OnRemovedFromContainer(Entity<TriggerOnRemovedFromContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        _trigger.Trigger(ent.Owner, args.Entity, ent.Comp.KeyOut);
    }

    // Used by entities to trigger when they are inserted into or removed from a container
    private void OnGotInsertedIntoContainer(Entity<TriggerOnGotInsertedIntoContainerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        _trigger.Trigger(ent.Owner, args.Entity, ent.Comp.KeyOut);
    }

    private void OnGotRemovedFromContainer(Entity<TriggerOnGotRemovedFromContainerComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        _trigger.Trigger(ent.Owner, args.Entity, ent.Comp.KeyOut);
    }
}

