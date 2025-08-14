using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Containers;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// System for creating a trigger when something is inserted into a container.
/// </summary>
public sealed class TriggerOnInsertedIntoContainer : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnInsertedIntoContainerComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
    }

    private void OnInsertedIntoContainer(Entity<TriggerOnInsertedIntoContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        _trigger.Trigger(ent.Owner, args.Entity, ent.Comp.KeyOut);
    }
}
