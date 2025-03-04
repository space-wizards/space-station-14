using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Labels.Components;
using Content.Shared.NameModifier.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Server.Containers;

namespace Content.Server.VoiceTrigger;

/// <summary>
/// Allows storages to be manipulated using voice commands.
/// </summary>
public sealed class StorageVoiceControlSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StorageVoiceControlComponent, VoiceTriggeredEvent>(VoiceTriggered);
    }

    private void VoiceTriggered(Entity<StorageVoiceControlComponent> ent, ref VoiceTriggeredEvent args)
    {
        // Check if the component has any slot restrictions via AllowedSlots
        // If it has slot restrictions, check if the item is in a slot that is allowed
        if (ent.Comp.AllowedSlots != null && _inventory.TryGetContainingSlot(ent.Owner, out var itemSlot) &&
            (itemSlot.SlotFlags & ent.Comp.AllowedSlots) == 0)
            return;

        // Don't do anything if there is no message
        if (args.Message == null)
            return;

        // Get the storage component
        if (!TryComp<StorageComponent>(ent, out var storage))
            return;

        // Get the hands component
        if (!TryComp<HandsComponent>(args.Source, out var hands))
            return;

        // If the player has something in their hands, try to insert it into the storage
        if (hands.ActiveHand != null && hands.ActiveHand.HeldEntity.HasValue)
        {
            // Disallow insertion and provide a reason why if the person decides to insert the item into itself
            if (ent.Owner.Equals(hands.ActiveHand.HeldEntity.Value))
            {
                _popup.PopupEntity(Loc.GetString("comp-storagevoicecontrol-self-insert", ("entity", hands.ActiveHand.HeldEntity.Value)), ent, args.Source);
                return;
            }
            if (_storage.CanInsert(ent, hands.ActiveHand.HeldEntity.Value, out var failedReason))
            {
                // We adminlog before insertion, otherwise the logger will attempt to pull info on an entity that no longer is present and throw an exception
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Source)} inserted {ToPrettyString(hands.ActiveHand.HeldEntity.Value)} into {ToPrettyString(ent)} via voice control");
                _storage.Insert(ent, hands.ActiveHand.HeldEntity.Value, out _);
                return;
            }
            {
                // Tell the player the reason why the item couldn't be inserted
                if (failedReason == null)
                    return;
                _popup.PopupEntity(Loc.GetString(failedReason), ent, args.Source);
                _adminLogger.Add(LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Source)} failed to insert {ToPrettyString(hands.ActiveHand.HeldEntity.Value)} into {ToPrettyString(ent)} via voice control");
            }
            return;
        }

        // If otherwise, we're retrieving an item, so check all the items currently in the attached storage
        foreach (var item in storage.Container.ContainedEntities)
        {
            // Some items could be labeled.
            // If they are, we need to check if both the label and the original name match the requester's message

            // Check and pull the LabelComponent and NameModifierComponent, if they exist, preform comparisons
            if (TryComp<LabelComponent>(item, out var itemLabelComponent) &&
                TryComp<NameModifierComponent>(item, out var itemNameModifierComponent))
            {
                if (args.Message.Contains(itemNameModifierComponent.BaseName,
                        StringComparison.InvariantCultureIgnoreCase) ||
                    itemLabelComponent.CurrentLabel != null && args.Message.Contains(itemLabelComponent.CurrentLabel,
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    ExtractItemFromStorage(ent, item, args.Source, hands);
                    break;
                }
            }

            // Check the item's regular name.
            // If the message doesn't match the item name the requestor requested, skip and move on to the next item
            if (args.Message.Contains(MetaData(item).EntityName, StringComparison.InvariantCultureIgnoreCase))
            {
                ExtractItemFromStorage(ent, item, args.Source, hands);
                break;
            }
        }
    }

    /// <summary>
    /// Extracts an item from storage and places it into the player's hands.
    /// </summary>
    /// <param name="ent">The entity of the item with the StorageVoiceControlComponent</param>
    /// <param name="item">The entity of the item to be extracted from the attached storage</param>
    /// <param name="source">The entity of the person wearing the item</param>
    /// <param name="hands">The HandsComponent of the person wearing the item</param>
    private void ExtractItemFromStorage(Entity<StorageVoiceControlComponent> ent,
        EntityUid item,
        EntityUid source,
        HandsComponent hands)
    {
        _container.RemoveEntity(ent, item);
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(source)} retrieved {ToPrettyString(item)} from {ToPrettyString(ent)} via voice control");
        _hands.TryPickup(source, item, handsComp: hands);
    }
}
