using Content.Server.Atmos.EntitySystems;
using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Roles;

namespace Content.Server._EE.Plasmaman;

public sealed partial class PlasmamanStartingGearSystem : EntitySystem
{
    private const string PlasmamanSpecies = "Plasmaman";

    private const string PocketTankPrototype = "DoubleEmergencyPlasmaTankFilled";

    private static readonly (string Slot, string Prototype)[] CriticalGear =
    {
        ("jumpsuit", "ClothingUniformEnvirosuit"),
        ("head", "ClothingHeadEnvirohelm"),
        ("gloves", "ClothingHandsGlovesEnvirogloves"),
        ("mask", "ClothingMaskBreath"),
        ("pocket1", PocketTankPrototype),
    };

    private static readonly string[] JumpsuitDependentSlots = { "pocket1", "pocket2" };

    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private StorageSystem _storage = default!;
    [Dependency] private GasTankSystem _gasTank = default!;
    [Dependency] private SharedInternalsSystem _internals = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, StartingGearEquippedEvent>(OnStartingGearEquipped);
    }

    private void OnStartingGearEquipped(EntityUid uid, HumanoidProfileComponent component, ref StartingGearEquippedEvent args)
    {
        if (component.Species != PlasmamanSpecies)
            return;

        _inventory.TryGetSlotEntity(uid, "id", out var id);

        var stash = StashJumpsuitDependents(uid);

        foreach (var (slot, prototype) in CriticalGear)
        {
            ReplaceSlot(uid, slot, prototype);
        }

        RestoreJumpsuitDependents(uid, stash);

        if (id != null &&
            !Deleted(id.Value) &&
            !_inventory.TryGetSlotEntity(uid, "id", out _))
        {
            _inventory.TryEquip(uid, id.Value, "id", silent: true, force: true);
        }

        TryHookInternals(uid);
    }

    private List<(string Slot, EntityUid Item)> StashJumpsuitDependents(EntityUid uid)
    {
        var stash = new List<(string, EntityUid)>();

        foreach (var slot in JumpsuitDependentSlots)
        {
            if (slot == "pocket1")
                continue; // pocket1 always becomes the plasma tank, drop its old contents.

            if (!_inventory.TryGetSlotEntity(uid, slot, out var item))
                continue;

            if (!_inventory.TryUnequip(uid, slot, silent: true, force: true, reparent: false))
                continue;

            stash.Add((slot, item.Value));
        }

        return stash;
    }

    private void RestoreJumpsuitDependents(EntityUid uid, List<(string Slot, EntityUid Item)> stash)
    {
        foreach (var (slot, item) in stash)
        {
            if (Deleted(item))
                continue;

            if (_inventory.TryEquip(uid, item, slot, silent: true, force: true))
                continue;

            if (TryStowInBackpack(uid, item))
                continue;

            if (TryComp<HandsComponent>(uid, out var hands) && _hands.TryPickup(uid, item, handsComp: hands))
                continue;

            Del(item);
        }
    }

    private bool TryStowInBackpack(EntityUid uid, EntityUid item)
    {
        if (!_inventory.TryGetSlotEntity(uid, "back", out var backEntity))
            return false;

        return _storage.Insert(backEntity.Value, item, out _, playSound: false);
    }

    private void ReplaceSlot(EntityUid uid, string slot, string prototype)
    {
        if (_inventory.TryGetSlotEntity(uid, slot, out var oldItem) &&
            _inventory.TryUnequip(uid, slot, out var removedItem, silent: true, force: true, reparent: false))
        {
            Del(removedItem ?? oldItem.Value);
        }

        var item = Spawn(prototype, Transform(uid).Coordinates);

        if (!_inventory.TryEquip(uid, item, slot, silent: true, force: true))
            Del(item);
    }

    private void TryHookInternals(EntityUid uid)
    {
        if (!TryComp<InternalsComponent>(uid, out var internals))
            return;

        if (_internals.AreInternalsWorking(internals))
            return;

        if (!_inventory.TryGetSlotEntity(uid, "pocket1", out var tankEnt))
            return;

        if (!TryComp<GasTankComponent>(tankEnt, out var gasTank))
            return;

        _gasTank.ConnectToInternals((tankEnt.Value, gasTank));
    }
}
