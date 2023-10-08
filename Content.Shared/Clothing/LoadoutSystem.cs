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

            // Spawn the loadout items
            var spawned = EntityManager.SpawnEntities(EntityManager.GetComponent<TransformComponent>(uid).Coordinates.ToMap(EntityManager), loadoutProto.Items!);

            foreach (var item in spawned)
            {
                if (EntityManager.TryGetComponent<ClothingComponent>(item, out var clothingComp) &&
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
                if (!_inventory.TryEquip(uid, item, slot, false, !string.IsNullOrEmpty(slot), true))
                    failedLoadouts.Add(item);
            }
        }

        // Return a list of items that couldn't be equipped so the server can handle it if it wants
        // The server has more information about the inventory system than the client does and the client doesn't need to put loadouts in backpacks
        return failedLoadouts;
    }


    /// <summary>
    ///     Returns whether or not a loadout prototype has any whitelist requirements
    /// </summary>
    /// <param name="loadout">The loadout prototype to get information from</param>
    public bool LoadoutWhitelistExists(LoadoutPrototype loadout)
    {
        return loadout.EntityWhitelist?.Components != null ||
               loadout.EntityWhitelist?.Tags != null ||
               loadout.JobWhitelist != null ||
               loadout.SpeciesWhitelist != null;
    }

    /// <summary>
    ///     Returns whether or not a loadout prototype has any blacklist requirements
    /// </summary>
    /// <param name="loadout">The loadout prototype to get information from</param>
    public bool LoadoutBlacklistExists(LoadoutPrototype loadout)
    {
        return loadout.EntityBlacklist?.Components != null ||
               loadout.EntityBlacklist?.Tags != null ||
               loadout.JobBlacklist != null ||
               loadout.SpeciesBlacklist != null;
    }


    /// <summary>
    ///     Gets a string describing the whitelist requirements of a loadout prototype OR an empty string if there are no whitelist requirements
    /// </summary>
    /// <param name="loadout">The loadout prototype to get information from</param>
    public string GetLoadoutWhitelistString(LoadoutPrototype loadout)
    {
        if (!LoadoutWhitelistExists(loadout))
            return "";

        var whitelist = new List<string> { Loc.GetString("humanoid-profile-editor-loadouts-whitelist") };


        if (loadout.EntityWhitelist?.Components != null)
        {
            foreach (var component in loadout.EntityWhitelist.Components)
            {
                whitelist.Add(Loc.GetString("humanoid-profile-editor-loadouts-component", ("component", component)));
            }
        }

        if (loadout.EntityWhitelist?.Tags != null)
        {
            foreach (var tag in loadout.EntityWhitelist.Tags)
            {
                whitelist.Add(Loc.GetString("humanoid-profile-editor-loadouts-tag", ("tag", tag)));
            }
        }

        if (loadout.JobWhitelist != null)
        {
            foreach (var job in loadout.JobWhitelist)
            {
                whitelist.Add(Loc.GetString("humanoid-profile-editor-loadouts-job", ("job", job)));
            }
        }

        if (loadout.SpeciesWhitelist != null)
        {
            foreach (var species in loadout.SpeciesWhitelist)
            {
                whitelist.Add(Loc.GetString("humanoid-profile-editor-loadouts-species", ("species", species)));
            }
        }


        return string.Join("\n ", whitelist);
    }

    /// <summary>
    ///     Gets a string describing the blacklist requirements of a loadout prototype OR an empty string if there are no blacklist requirements
    /// </summary>
    /// <param name="loadout">The loadout prototype to get information from</param>
    public string GetLoadoutBlacklistString(LoadoutPrototype loadout)
    {
        if (!LoadoutBlacklistExists(loadout))
            return "";

        var blacklist = new List<string> { Loc.GetString("humanoid-profile-editor-loadouts-blacklist") };


        if (loadout.EntityBlacklist?.Components != null)
        {
            foreach (var component in loadout.EntityBlacklist.Components)
            {
                blacklist.Add(Loc.GetString("humanoid-profile-editor-loadouts-component", ("component", component)));
            }
        }

        if (loadout.EntityBlacklist?.Tags != null)
        {
            foreach (var tag in loadout.EntityBlacklist.Tags)
            {
                blacklist.Add(Loc.GetString("humanoid-profile-editor-loadouts-tag", ("tag", tag)));
            }
        }

        if (loadout.JobBlacklist != null)
        {
            foreach (var job in loadout.JobBlacklist)
            {
                blacklist.Add(Loc.GetString("humanoid-profile-editor-loadouts-job", ("job", job)));
            }
        }

        if (loadout.SpeciesBlacklist != null)
        {
            foreach (var species in loadout.SpeciesBlacklist)
            {
                blacklist.Add(Loc.GetString("humanoid-profile-editor-loadouts-species", ("species", species)));
            }
        }


        return string.Join("\n ", blacklist);
    }
}
