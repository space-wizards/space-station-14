using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Server.Containers;

namespace Content.Server.VoiceTrigger;

/// <summary>
/// Allows storages to be manipulated using voice commands.
/// </summary>
public sealed class StorageVoiceControlSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlot = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

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
        if (!TryComp<HandsComponent>(args.Source, out var hands))
            return;

        // If the player has something in their hands, try to insert it into the storage
        // TODO: The player could have something in their hands like a tool, but they really requested an item from storage, so this logic is flawed
        if (hands.ActiveHand != null && hands.ActiveHand.HeldEntity.HasValue)
        {
            if (_storage.CanInsert(ent, hands.ActiveHand.HeldEntity.Value, out var failedReason))
            {
                // We adminlog before insertion, otherwise the logger will attempt to pull info on an entity that no longer is present and throw an exception
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Source)} inserted {ToPrettyString(hands.ActiveHand.HeldEntity.Value)} into {ToPrettyString(ent)} via voice control");
                _storage.Insert(ent, hands.ActiveHand.HeldEntity.Value, out _);
            }
            else
            {
                // Tell the player the reason why the item couldn't be inserted
                if (failedReason != null)
                    _popup.PopupEntity(Loc.GetString(failedReason), ent);
                    _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Source)} failed to insert {ToPrettyString(hands.ActiveHand.HeldEntity.Value)} into {ToPrettyString(ent)} via voice control");

            }
            return;
        }

        // If otherwise, we're retrieving an item, so check all the items currently in the attached storage
        foreach (var item in storage.Container.ContainedEntities)
        {
            // Get the metadata component so we can check for the item name, we do this because the name on the entity is private
            TryComp<MetaDataComponent>(item, out var metaData);

            // The message doesn't match the item name the requestor requested, skip and move on to the next item
            var itemName = metaData?.EntityName.ToString();
            if (itemName == null || !args.Message.Contains(itemName, StringComparison.InvariantCultureIgnoreCase))
                continue;

            // We found the item we want, so draw it from storage and place it into the player's hands
            if (storage.Container.ContainedEntities.Count != 0)
            {
                _container.RemoveEntity(ent, item);
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Source)} retrieved {itemName} from {ToPrettyString(ent)} via voice control");
                _hands.TryPickup(args.Source, item, handsComp: hands);
                break;
            }
        }
    }
}
