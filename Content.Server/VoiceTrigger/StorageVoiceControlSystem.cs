using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
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
            // Check if the name contains the actual command.
            // This will do comparisons against any length of string which is a little weird, but worth the tradeoff.
            // E.g "go go s" would give you the screwdriver because "screwdriver" contains "s"
            if (Name(item).Contains(args.MessageWithoutPhrase))
            {
                ExtractItemFromStorage(ent, item, args.Source, hands);
                break;
            }
        }
    }

    /// <summary>
    /// Extracts an item from storage and places it into the player's hands.
    /// </summary>
    /// <param name="ent">The entity with the <see cref="StorageVoiceControlComponent"/></param>
    /// <param name="item">The entity to be extracted from the attached storage</param>
    /// <param name="source">The entity wearing the item</param>
    /// <param name="hands">The <see cref="HandsComponent"/> of the person wearing the item</param>
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
