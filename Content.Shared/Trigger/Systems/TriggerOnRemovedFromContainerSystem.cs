using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Containers;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// System for creating a trigger when something is removed from a container.
/// </summary>
public sealed class TriggerOnRemovedFromContainer : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnRemovedFromContainerComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    private void OnRemovedFromContainer(Entity<TriggerOnRemovedFromContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        _trigger.Trigger(ent.Owner, args.Entity, ent.Comp.KeyOut);
    }
}
