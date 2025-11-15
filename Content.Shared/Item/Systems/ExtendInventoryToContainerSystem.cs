using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Item.Components;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.Item.Systems;

public sealed class ExtendInventoryToContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExtendInventoryToContainerComponent, InsertIntoItemSlotEvent>(OnItemInsertion);
        SubscribeLocalEvent<ExtendedInventoryComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ExtendInventoryToContainerComponent, EjectFromItemSlotEvent>(OnItemEjection);
    }

    private void OnItemInsertion(Entity<ExtendInventoryToContainerComponent> ent, ref InsertIntoItemSlotEvent args)
    {
        var inventory = ent.Owner;
        EnsureComp<ExtendedInventoryComponent>(args.Container, out var extendedInventory);
        extendedInventory.ConnectedContainer.Add(inventory);
    }

    private void OnInteractUsing(Entity<ExtendedInventoryComponent> ent, ref InteractUsingEvent args)
    {
        foreach (var container in ent.Comp.ConnectedContainer)
        {
            if (!TryComp<StorageComponent>(container, out var storage)
                || !_containers.Insert(args.Used, storage.Container))
                continue;

            _audioSystem.PlayPredicted(storage.StorageInsertSound, args.Target, args.User);
            return; // Break the Foreach after the item has been inserted.
        }
    }

    private void OnItemEjection(Entity<ExtendInventoryToContainerComponent> ent, ref EjectFromItemSlotEvent args)
    {
        if (!TryComp<ExtendedInventoryComponent>(args.Container, out var extendedInventory))
            return;
        extendedInventory.ConnectedContainer.Remove(ent.Comp.Owner);

        // Remove the Component if no containers are connected.
        if (extendedInventory.ConnectedContainer.Count == 0)
            RemComp<ExtendedInventoryComponent>(args.Container);
    }
}
