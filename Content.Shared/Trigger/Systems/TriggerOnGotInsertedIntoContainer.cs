using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Containers;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// System for creating a trigger when an entity gets inserted into a container.
/// </summary>
public sealed class TriggerOnGotInsertedIntoContainer : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnGotInsertedIntoContainerComponent, EntGotInsertedIntoContainerMessage>(OnGotInsertedIntoContainer);
    }

    private void OnGotInsertedIntoContainer(Entity<TriggerOnGotInsertedIntoContainerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        _trigger.Trigger(ent.Owner, args.Entity, ent.Comp.KeyOut);
    }
}
