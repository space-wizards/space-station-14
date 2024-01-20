using Content.Server.GameTicking;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Configuration;

namespace Content.Server.Clothing.Systems;

public sealed class LoadoutSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly Shared.Clothing.LoadoutSystem _loadout = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }


    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId == null ||
            !_configurationManager.GetCVar(CCVars.GameLoadoutsEnabled))
            return;

        // Spawn the loadout, get a list of items that failed to equip
        var failedLoadouts = _loadout.ApplyCharacterLoadout(ev.Mob, ev.JobId, ev.Profile, _playTimeTracking.GetTrackerTimes(ev.Player));

        // Try to find back-mounted storage apparatus
        if (!_inventory.TryGetSlotEntity(ev.Mob, "back", out var item) ||
            !EntityManager.TryGetComponent<StorageComponent>(item, out var inventory))
            return;

        // Try inserting the entity into the storage, if it can't, it leaves the loadout item on the ground
        foreach (var loadout in failedLoadouts)
        {
            if (EntityManager.TryGetComponent<ItemComponent>(loadout, out var itemComp) &&
                _storage.CanInsert(item.Value, loadout, out _, inventory, itemComp))
                _storage.Insert(item.Value, loadout, out _);
        }
    }
}
