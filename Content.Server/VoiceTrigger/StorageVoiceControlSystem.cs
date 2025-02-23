using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Storage;
using Robust.Server.Containers;

namespace Content.Server.VoiceTrigger;

/// <summary>
/// Allows items slots to be manipulated using voice commands
/// </summary>
public sealed class StorageVoiceControlSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlot = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StorageVoiceControlComponent, VoiceTriggeredEvent>(VoiceTriggered);
    }

    private void VoiceTriggered(Entity<StorageVoiceControlComponent> ent, ref VoiceTriggeredEvent args)
    {
        // Don't do anything if there is no message.
        if (args.Message == null)
            return;

        // Get the storage component
        if (!TryComp<StorageComponent>(ent, out var storage))
            return;

        // Get the hands component
        if (!TryComp<HandsComponent>(args.Source, out var hands));
            return;

        // Check all the items currently in the attached storage
        foreach (var item in storage.Container.ContainedEntities)
        {
            // The message doesn't match the item name, skip
            if (!args.Message.Contains(item.ToString(), StringComparison.InvariantCultureIgnoreCase))
                continue;

            // Insert into storage in itemSlot (I don't think this is working still, either way its not efficient)
            if (hands.ActiveHand != null && hands.ActiveHand.HeldEntity.HasValue && _storage.CanInsert(item, hands.ActiveHand.HeldEntity.Value, out _))
            {
                _storage.Insert(item, hands.ActiveHand.HeldEntity.Value, out _);
                break;
            }

            // Draw from storage in itemSlot
            if (storage.Container.ContainedEntities.Count != 0)
            {
                var removing = storage.Container.ContainedEntities[^1]; _container.RemoveEntity(item, removing); _hands.TryPickup(args.Source, removing, handsComp: hands);
                break;
            }


            // Insert direct into itemSlot
            if (_container.Insert(item, storage.Container, new TransformComponent()));
                break;

            // Draw from itemSlot direct
            if (_container.TryRemoveFromContainer(ent));
                break;

        }
    }
}
