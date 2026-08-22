using Robust.Shared.Containers;

namespace Content.Shared.Item;

/// <summary>
/// Handles items that can swap places with another item stashed inside them, e.g. a pair of gloves that
/// swap for a hidden fingergun. See <see cref="TransformableItemComponent"/>.
/// </summary>
public sealed partial class TransformableItemSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TransformableItemComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<TransformableItemComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Prototype == null)
            return;

        PredictedTrySpawnInContainer(ent.Comp.Prototype, ent, ent.Comp.ContainerId, out _);
    }

    /// <summary>
    /// Gets the item currently stashed inside <paramref name="uid"/>
    /// </summary>
    public bool TryGetHiddenItem(Entity<TransformableItemComponent?> uid, out EntityUid hidden)
    {
        hidden = default;

        if (!Resolve(uid, ref uid.Comp))
            return false;

        if (!_containers.TryGetContainer(uid, uid.Comp.ContainerId, out var container) || container.ContainedEntities.Count == 0)
            return false;

        hidden = container.ContainedEntities[0];
        return true;
    }

    /// <summary>
    /// Swaps item active for the item stashed inside it, placing the stashed item
    /// where the active item was and viceversa
    /// </summary>
    public void Swap(EntityUid visible, Entity<TransformableItemComponent?> hidden)
    {
        if (!Resolve(hidden, ref hidden.Comp))
            return;

        if (!_containers.TryGetContainingContainer((visible, null, null), out var handContainer) ||
            !_containers.TryGetContainingContainer((hidden.Owner, null, null), out var stashOnVisible) ||
            !_containers.TryGetContainer(hidden, hidden.Comp.ContainerId, out var stashOnHidden))
            return;

        _containers.Remove(hidden.Owner, stashOnVisible, reparent: false);
        _containers.Remove(visible, handContainer, reparent: false);
        _containers.Insert(hidden.Owner, handContainer);
        _containers.Insert(visible, stashOnHidden);
    }
}
