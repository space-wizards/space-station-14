using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Loadouts;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Clothing;


public sealed class LoadoutSystem : EntitySystem
{
    [Dependency] private readonly SharedStationSpawningSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoadoutComponent, MapInitEvent>(OnMapInit);
    }


    private void OnMapInit(EntityUid uid, LoadoutComponent component, MapInitEvent args)
    {
        if (component.Prototypes == null)
            return;

        var proto = _prototype.Index<StartingGearPrototype>(_random.Pick(component.Prototypes));
        _station.EquipStartingGear(uid, proto, null);
    }


    /// <summary>
    ///     Equips entities from a <see cref="HumanoidCharacterProfile"/>'s loadout preferences to a given entity
    /// </summary>
    /// <param name="uid">The entity to give the loadout items to</param>
    /// <param name="job">The job to use for loadout whitelist/blacklist (should be the job of the entity)</param>
    /// <param name="profile">The profile to get loadout items from (should be the entity's, or at least have the same species as the entity)</param>
    /// <returns>A list of loadout items that couldn't be equipped but passed checks</returns>
    public List<EntityUid> ApplyCharacterLoadout(EntityUid uid, JobPrototype job, HumanoidCharacterProfile profile)
    {
        var failedLoadouts = new List<EntityUid>();

        foreach (var loadout in profile.LoadoutPreferences)
        {
            var slot = "";

            // Ignore loadouts that don't exist
            if (!_prototype.TryIndex<LoadoutPrototype>(loadout, out var loadoutProto))
                continue;

            // Check whitelists and blacklists for this loadout
            if (loadoutProto.EntityWhitelist != null && !loadoutProto.EntityWhitelist.IsValid(uid) ||
                loadoutProto.EntityBlacklist != null && loadoutProto.EntityBlacklist.IsValid(uid) ||
                loadoutProto.JobWhitelist != null && !loadoutProto.JobWhitelist.Contains(job.ID) ||
                loadoutProto.JobBlacklist != null && loadoutProto.JobBlacklist.Contains(job.ID) ||
                loadoutProto.SpeciesWhitelist != null && !loadoutProto.SpeciesWhitelist.Contains(profile.Species) ||
                loadoutProto.SpeciesBlacklist != null && loadoutProto.SpeciesBlacklist.Contains(profile.Species))
                continue;

            // Spawn the loadout item
            var spawned = EntityManager.SpawnEntity(loadoutProto.Item, EntityManager.GetComponent<TransformComponent>(uid).Coordinates);


            if (EntityManager.TryGetComponent<ClothingComponent>(spawned, out var clothingComp) &&
                _inventory.TryGetSlots(uid, out var slotDefinitions))
            {
                var deleted = false;
                foreach (var curSlot in slotDefinitions)
                {
                    // If the loadout can't equip here or we've already deleted an item from this slot, skip it
                    if (!clothingComp.Slots.HasFlag(curSlot.SlotFlags) || deleted)
                        continue;

                    slot = curSlot.Name;

                    // If the loadout is exclusive delete the equipped item
                    if (loadoutProto.Exclusive)
                    {
                        // Get the item in the slot
                        if (!_inventory.TryGetSlotEntity(uid, curSlot.Name, out var slotItem))
                            continue;

                        EntityManager.DeleteEntity(slotItem.Value);
                        deleted = true;
                    }
                }
            }


            // Equip the loadout
            if (!_inventory.TryEquip(uid, spawned, slot, false, !string.IsNullOrEmpty(slot), true))
                failedLoadouts.Add(spawned);
        }

        // Return a list of items that couldn't be equipped so the server can handle it if it wants
        // The server has more information about the inventory system than the client does and the client doesn't need to put loadouts in backpacks
        return failedLoadouts;
    }
}
