using System.Diagnostics.CodeAnalysis;
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
public sealed class ItemSlotVoiceControlSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlot = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly StorageSystem _storage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemSlotVoiceControlComponent, VoiceTriggeredEvent>(VoiceTriggered);
    }

    private void VoiceTriggered(Entity<ItemSlotVoiceControlComponent> ent, ref VoiceTriggeredEvent args)
    {
        if (args.Message == null)
            return;

        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;

        // checks the name of each item slot, if the name was stated in the message that triggered the TriggerOnVoice then interact
        foreach (var slot in slots.Slots.Values)
        {
            if (!args.Message.Contains(slot.Name, StringComparison.InvariantCultureIgnoreCase))
                continue;

            if (slot.HasItem &&
                TryComp<StorageComponent>(slot.Item, out var storage) &&
                TryComp<HandsComponent>(args.Source, out var hands))
            {
                var slotItem = slot.Item.Value;

                // Insert into storage in itemSlot (I don't think this is working still, either way its not efficient)
                if (hands.ActiveHand != null &&
                    hands.ActiveHand.HeldEntity.HasValue &&
                    _storage.CanInsert(slotItem,hands.ActiveHand.HeldEntity.Value, out _))
                {
                    _storage.Insert(slotItem, hands.ActiveHand.HeldEntity.Value, out _);
                    break;
                }

                // Draw from storage in itemSlot
                if (storage.Container.ContainedEntities.Count != 0)
                {
                    var removing = storage.Container.ContainedEntities[^1];
                    _container.RemoveEntity(slotItem, removing);
                    _hands.TryPickup(args.Source, removing, handsComp: hands);
                    break;
                }
            }

            // Insert direct into itemSlot
            if (_itemSlot.TryInsertFromHand(ent, slot, args.Source))
                break;

            // Draw from itemSlot direct
            if (_itemSlot.TryEjectToHands(ent, slot, args.Source))
                break;

        }
    }
}
