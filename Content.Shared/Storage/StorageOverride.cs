using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Storage;

/// <summary>
/// System for searching down storage hierarchies to find items to replace.
/// </summary>
public sealed partial class StorageOverrideSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedStorageSystem _storageSystem = default!;

    // TODO: Make new prototype to compare against!
    private readonly string _tempProtoSpecies = "SlimePerson";
    private readonly string _tempProtoSlotName = "back";
    private readonly int _tempProtoMaxReplacements = int.MaxValue;
    private readonly bool _tempProtoSearchReplaced = true; // Should search continue inside replaced containers?
    private readonly Dictionary<string, string> _tempProtoPairs = new()
    {
        { "EmergencyOxygenTankFilled", "EmergencyNitrogenTankFilled" },
        { "ExtendedEmergencyOxygenTankFilled", "ExtendedEmergencyNitrogenTankFilled" },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ApplyStorageOverrideEvent>(ApplyStorageOverride);
    }

    /// <summary>
    /// Contextual checks to determine whether a storage container may have something we wish to replace.
    /// </summary>
    private void ApplyStorageOverride(ApplyStorageOverrideEvent ev)
    {
        if (!EntityManager.HasComponent<StorageComponent>(ev.Entity))
            return;

        // TODO: Make new prototype to compare against!
        if (ev.Profile?.Species != _tempProtoSpecies)
            return;

        // TODO: Make new prototype to compare against!
        if (string.IsNullOrEmpty(ev.Slot?.Name) || ev.Slot?.Name != _tempProtoSlotName)
            return;

        RecursiveStorageOverride(ev.Entity);
    }

    /// <summary>
    /// A recursive search through a storage container.
    /// </summary>
    /// <param name="item">The item in question. On first pass, this is the root.</param>
    /// <param name="root">Top level entity for items found in containers.</param>
    /// <param name="container">The container the item is inside, if any.</param>
    /// <param name="location">The location of the item inside the container, if any.</param>
    private void RecursiveStorageOverride(EntityUid item, int? replacements = 0, EntityUid? root = null, Container? container = null, ItemStorageLocation? location = null)
    {
        replacements ??= 0;

        if (EntityManager.TryGetComponent<MetaDataComponent>(item, out var metadataComp) &&
            ReplaceItemByPrototype(item, root, metadataComp.EntityPrototype?.ID, container, location))
        {
            if (++replacements >= _tempProtoMaxReplacements)
                return;

            if (!_tempProtoSearchReplaced)
                return;
        }

        if (EntityManager.TryGetComponent<StorageComponent>(item, out var storageComp))
        {
            foreach (var (uid, loc) in new Dictionary<EntityUid, ItemStorageLocation>(storageComp.StoredItems))
                RecursiveStorageOverride(uid, replacements, root, storageComp.Container, loc);
        }
    }

    /// <summary>
    /// Replaces an item if it matches by prototype id. Handles the item's container if necessary.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <param name="root">Top level entity for items found in containers.</param>
    /// <param name="id">The prototype id of the item to spawn and insert.</param>
    /// <param name="container">The container the item is inside, if any.</param>
    /// <param name="location">The location of the item inside the container, if any.</param>
    private bool ReplaceItemByPrototype(EntityUid item, EntityUid? root, string? id, Container? container = null, ItemStorageLocation? location = null)
    {
        // TODO: Make new prototype to compare against!
        if (string.IsNullOrEmpty(id) || !_tempProtoPairs.TryGetValue(id, out var newID))
            return false;

        var newItem = Spawn(newID, EntityManager.GetComponent<TransformComponent>(root ?? item).Coordinates);

        if (container != null)
        {
            DebugTools.Assert(root != item);
            DebugTools.Assert(location != null);

            _containerSystem.Remove(item, container);
            _storageSystem.InsertAt(container.Owner, newItem, location.Value, out var _, playSound: false);
        }

        EntityManager.QueueDeleteEntity(item);

        return true;
    }
}

/// <summary>
/// An event directed at a storage container to perform a recursive search and replacement of it or it's contents.
/// </summary>
/// <param name="profile">Character profile for the player the storage container is about to be equipped to, if any.</param>
/// <param name="slotName">Name of the inventory slot on the player the storage container is about to be equipped to, if any.</param>
public sealed class ApplyStorageOverrideEvent : EntityEventArgs
{
    public EntityUid Entity { get; }
    public HumanoidCharacterProfile? Profile { get; }
    public SlotDefinition? Slot { get; }

    public ApplyStorageOverrideEvent(EntityUid entity, HumanoidCharacterProfile? profile = null, SlotDefinition? slot = null)
    {
        Entity = entity;
        Profile = profile;
        Slot = slot;
    }
}
