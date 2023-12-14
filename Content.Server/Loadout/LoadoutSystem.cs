using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Loadout;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Ganimed.SponsorManager;

namespace Content.Server.Loadout;

public sealed class LoadoutSystem : EntitySystem
{
    private const string BackpackSlotId = "back";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly StorageSystem _storageSystem = default!;
	[Dependency] private readonly SponsorManager _sponsorManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
		int loadoutTotal = 0;
		int loadoutMax = !(ev.Player is null) && !(ev.Player.ConnectedClient is null) 
			&& _sponsorManager.AllowSponsor(ev.Player)
				? 20 : 14;
		
		if (ev.JobId is null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job) || !job.DoLoadout)
			return;
		
		foreach (var loadoutId in ev.Profile.LoadoutPreferences)
        {
			if (!_prototypeManager.TryIndex<LoadoutPrototype>(loadoutId, out var loadout))
                continue;
			loadoutTotal += loadout.Cost;
			if (loadoutMax - loadoutTotal < 0) 
			{
				return;
			}
		}
		
		foreach (var loadoutId in ev.Profile.LoadoutPreferences)
        {
            if (!_prototypeManager.TryIndex<LoadoutPrototype>(loadoutId, out var loadout))
                continue;
            var isWhitelisted = ev.JobId == null ||
                                loadout.WhitelistJobs != null &&
                                !loadout.WhitelistJobs.Contains(ev.JobId);
            var isBlacklisted = ev.JobId != null &&
                                loadout.BlacklistJobs != null &&
                                loadout.BlacklistJobs.Contains(ev.JobId);
			var isSponsor = !(ev.Player is null) &&
                                _sponsorManager.AllowSponsor(ev.Player);
			var sponsorRestriction = !isSponsor && loadout.SponsorOnly;
            var isSpeciesRestricted = loadout.SpeciesRestrictions != null &&
                                      loadout.SpeciesRestrictions.Contains(ev.Profile.Species);

            if (isWhitelisted || isBlacklisted || isSpeciesRestricted || sponsorRestriction)
                continue;

            var entity = Spawn(loadout.Prototype, Transform(ev.Mob).Coordinates);

            // Take in hand if not clothes
            if (!TryComp<ClothingComponent>(entity, out var clothing))
            {
                _handsSystem.TryPickup(ev.Mob, entity);
                continue;
            }

            // Automatically search empty slot for clothes to equip
            string? firstSlotName = null;
            var isEquipped = false;
			if (_inventorySystem.TryGetSlots(ev.Mob, out var slotDefinitions))
			{
				foreach (var slot in slotDefinitions)
				{
					if (!clothing.Slots.HasFlag(slot.SlotFlags))
						continue;

					firstSlotName ??= slot.Name;

					if (_inventorySystem.TryGetSlotEntity(ev.Mob, slot.Name, out var _))
						continue;

					if (loadout.Exclusive && _inventorySystem.TryUnequip(ev.Mob, firstSlotName, out var removedItem, true, true))
						_entityManager.DeleteEntity(removedItem.Value);

					if (!_inventorySystem.TryEquip(ev.Mob, entity, slot.Name, true, loadout.Exclusive))
						continue;

					isEquipped = true;
					break;
				}
				
				if (isEquipped || firstSlotName == null)
					continue;

				// Force equip to first valid clothes slot
				// Get occupied entity -> Insert to backpack -> Equip loadout entity
				if (_inventorySystem.TryGetSlotEntity(ev.Mob, firstSlotName, out var slotEntity) &&
					_inventorySystem.TryGetSlotEntity(ev.Mob, BackpackSlotId, out var backEntity) &&
					_storageSystem.CanInsert(backEntity.Value, slotEntity.Value, out _))
				{
					_storageSystem.Insert(backEntity.Value, slotEntity.Value, out _);
				}

				_inventorySystem.TryEquip(ev.Mob, entity, firstSlotName, true);
			}
        }
    }
}
