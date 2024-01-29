using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Storage;

public sealed partial class StorageOverrideSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    // TODO: Make new prototype to compare against!
    private readonly string _tempProtoSpecies = "SlimePerson";
    private readonly string _tempProtoSlotName = "back";
    private readonly Dictionary<string, string> _tempProtoPairs = new()
    {
        { "EmergencyOxygenTankFilled", "EmergencyNitrogenTankFilled" },
        { "ExtendedEmergencyOxygenTankFilled", "ExtendedEmergencyNitrogenTankFilled" },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaDataComponent, ApplyStorageOverrideEvent>(ApplyStorageOverride);
    }

    /// <summary>
    /// Contextual checks to determine whether an item may be something or contain something we wish to replace.
    /// </summary>
    private void ApplyStorageOverride(Entity<MetaDataComponent> item, ref ApplyStorageOverrideEvent ev)
    {
        // TODO: Make new prototype to compare against!
        if (ev.Profile?.Species != _tempProtoSpecies)
            return;

        // TODO: Make new prototype to compare against!
        if (string.IsNullOrEmpty(ev.Slot?.Name) || ev.Slot?.Name != _tempProtoSlotName)
            return;

        RecursiveStorageOverride(item);
    }

    /// <summary>
    /// A recursive search through an item or it's storage container.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <param name="root">Top level entity for items found in containers.</param>
    /// <param name="container">The container the item is inside, if any.</param>
    /// <param name="location">The location of the item inside the container, if any.</param>
    private void RecursiveStorageOverride(EntityUid item, EntityUid? root = null, Container? container = null, ItemStorageLocation? location = null)
    {
        if (_entityManager.TryGetComponent<StorageComponent>(item, out var storageComp))
        {
            foreach (var (uid, loc) in new Dictionary<EntityUid, ItemStorageLocation>(storageComp.StoredItems))
                RecursiveStorageOverride(uid, root, storageComp.Container, loc);
        }
        else if (_entityManager.TryGetComponent<MetaDataComponent>(item, out var metadataComp))
        {
            ReplaceItemByPrototype(item, root, metadataComp.EntityPrototype?.ID, container, location);
        }
    }

    /// <summary>
    /// Replaces an item if it matches by prototype id, and if necessary, inside of a container.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <param name="root">Top level entity for items found in containers.</param>
    /// <param name="id">The prototype id of the item to spawn and insert.</param>
    /// <param name="container">The container the item is inside, if any.</param>
    /// <param name="location">The location of the item inside the container, if any.</param>
    private void ReplaceItemByPrototype(EntityUid item, EntityUid? root, string? id, Container? container = null, ItemStorageLocation? location = null)
    {
        // TODO: Make new prototype to compare against!
        if (string.IsNullOrEmpty(id) || !_tempProtoPairs.TryGetValue(id, out var newID))
            return;

        var newItem = Spawn(newID, _entityManager.GetComponent<TransformComponent>(root ?? item).Coordinates);

        if (container == null)
        {
            // Top level, we never recursed
            DebugTools.Assert(root == item);
            DebugTools.Assert(location == null);
        }
        else
        {
            _containerSystem.Remove(item, container);
            _containerSystem.Insert(newItem, container); // TODO: Use location to make sure the item is in the same spot
        }

        _entityManager.QueueDeleteEntity(item);
    }
}

/// <summary>
/// An event directed at an item to perform a recursive search and replacement of it or it's contents.
/// </summary>
/// <param name="profile">Character profile for the player the item is about to be equipped to, if any.</param>
/// <param name="slotName">Name of the inventory slot on the player the item is about to be equipped to, if any.</param>
public sealed class ApplyStorageOverrideEvent : EntityEventArgs
{
    public HumanoidCharacterProfile? Profile { get; }
    public SlotDefinition? Slot { get; }

    public ApplyStorageOverrideEvent(HumanoidCharacterProfile? profile = null, SlotDefinition? slot = null)
    {
        Profile = profile;
        Slot = slot;
    }
}
