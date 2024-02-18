using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Loadouts;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing;


public sealed class LoadoutSystem : EntitySystem
{
    [Dependency] private readonly SharedStationSpawningSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

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


    /// <inheritdoc cref="ApplyCharacterLoadout(Robust.Shared.GameObjects.EntityUid,string,Content.Shared.Preferences.HumanoidCharacterProfile,System.Collections.Generic.Dictionary{string,System.TimeSpan}?)"/>
    public List<EntityUid> ApplyCharacterLoadout(EntityUid uid, string job, HumanoidCharacterProfile profile,
        Dictionary<string, TimeSpan>? playTimes = null)
    {
        var jobPrototype = _prototype.Index<JobPrototype>(job);
        return ApplyCharacterLoadout(uid, jobPrototype, profile, playTimes);
    }

    /// <summary>
    ///     Equips entities from a <see cref="HumanoidCharacterProfile"/>'s loadout preferences to a given entity
    /// </summary>
    /// <param name="uid">The entity to give the loadout items to</param>
    /// <param name="job">The job to use for loadout whitelist/blacklist (should be the job of the entity)</param>
    /// <param name="profile">The profile to get loadout items from (should be the entity's, or at least have the same species as the entity)</param>
    /// <param name="playTimes">Playtime for the player for use with playtime requirements</param>
    /// <returns>A list of loadout items that couldn't be equipped but passed checks</returns>
    public List<EntityUid> ApplyCharacterLoadout(EntityUid uid, JobPrototype job, HumanoidCharacterProfile profile,
        Dictionary<string, TimeSpan>? playTimes = null)
    {
        var failedLoadouts = new List<EntityUid>();

        foreach (var loadout in profile.LoadoutPreferences)
        {
            var slot = "";

            // Ignore loadouts that don't exist
            if (!_prototype.TryIndex<LoadoutPrototype>(loadout, out var loadoutProto))
                continue;


            if (!CheckRequirementsValid(loadoutProto.Requirements, job, profile,
                playTimes ?? new Dictionary<string, TimeSpan>(), EntityManager, _prototype, _configurationManager,
                out _))
                continue;


            // Spawn the loadout items
            var spawned = EntityManager.SpawnEntities(
                EntityManager.GetComponent<TransformComponent>(uid).Coordinates.ToMap(EntityManager),
                loadoutProto.Items!);

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


    public bool CheckRequirementsValid(List<LoadoutRequirement> requirements, JobPrototype job,
        HumanoidCharacterProfile profile, Dictionary<string, TimeSpan> playTimes, IEntityManager entityManager,
        IPrototypeManager prototypeManager, IConfigurationManager configManager, out List<FormattedMessage> reasons)
    {
        reasons = new List<FormattedMessage>();
        var valid = true;

        foreach (var requirement in requirements)
        {
            // set valid to false if the requirement is invalid and not inverted, if it's inverted set it to true when it's valid
            if (!requirement.IsValid(job, profile, playTimes, entityManager, prototypeManager, configManager, out var reason))
            {
                if (valid)
                    valid = requirement.Inverted;
            }
            else
            {
                if (valid)
                    valid = !requirement.Inverted;
            }

            if (reason != null)
            {
                reasons.Add(reason);
            }
        }

        return valid;
    }
}
