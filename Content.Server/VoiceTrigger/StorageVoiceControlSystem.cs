using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory.Events;
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StorageVoiceControlComponent, VoiceTriggeredEvent>(VoiceTriggered);
        SubscribeLocalEvent<StorageVoiceControlComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<StorageVoiceControlComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<StorageVoiceControlComponent, ComponentInit>(OnComponentInit);
    }

    private static void OnComponentInit(EntityUid uid, StorageVoiceControlComponent component, ComponentInit args)
    {
        // We do this explicitly because without this logic, the system would behave in weird ways.
        // For example, if AllowedSlots is null, the component can't be used unless someone equips it to a slot,
        // triggering the GotEquippedEvent to enable it.
        component.IsFunctional = component.AllowedSlots == null;
    }

    private static void OnGotUnequipped(Entity<StorageVoiceControlComponent> ent, ref GotUnequippedEvent args)
    {
        // Don't disable the component if we don't have allowed slots
        ent.Comp.IsFunctional = ent.Comp.AllowedSlots == null;
    }

    private static void OnGotEquipped(Entity<StorageVoiceControlComponent> ent, ref GotEquippedEvent args)
    {
        // If we don't have allowed slots, it can be used anywhere, so enable functionality.
        // If we do have allowed slots, check if we're equipped to one of them. If we're not, disable functionality.
        ent.Comp.IsFunctional = ent.Comp.AllowedSlots == null || (args.SlotFlags & ent.Comp.AllowedSlots) != 0;
    }

    private void VoiceTriggered(Entity<StorageVoiceControlComponent> ent, ref VoiceTriggeredEvent args)
    {
        // Don't do anything if we've disabled the component
        if (!ent.Comp.IsFunctional)
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
                _popup.PopupEntity(Loc.GetString("You can't insert an item into itself!"), ent, args.Source);
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
            // Get the item's name
            var itemName = MetaData(item).EntityName;
            // The message doesn't match the item name the requestor requested, skip and move on to the next item
            if (!args.Message.Contains(itemName, StringComparison.InvariantCultureIgnoreCase))
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
