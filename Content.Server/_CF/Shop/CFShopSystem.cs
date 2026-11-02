using System.Numerics;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.CF.Combat;
using Content.Shared.CF.Shop;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.CF.Shop;

/// <summary>
/// Spawns accepted shop lines onto the player: an empty hand, then the backpack, then the ground nearby.
/// </summary>
public sealed class CFShopSystem : EntitySystem
{
    private const int MaxItems = 100;
    private const string BackSlot = "back";

    private static readonly Vector2[] DropOffsets =
    {
        new(0.6f, 0f),
        new(-0.6f, 0f),
        new(0f, 0.6f),
        new(0f, -0.6f),
        new(0.6f, 0.6f),
        new(-0.6f, 0.6f),
        new(0.6f, -0.6f),
        new(-0.6f, -0.6f),
    };

    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private int _drop;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<CFShopPurchaseEvent>(OnPurchase);
    }

    private void OnPurchase(CFShopPurchaseEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var purse = EnsureComp<CFPurseComponent>(user);
        if (!purse.Seeded)
        {
            var level = TryComp<CFCombatantComponent>(user, out var combatant) ? combatant.Level : 1;
            purse.Copper = CFCoins.StartingCopper(level);
            purse.Seeded = true;
        }

        var plan = new List<(string Prototype, int Count)>();
        var total = 0;
        var remaining = MaxItems;
        foreach (var line in ev.Lines)
        {
            if (remaining <= 0)
                break;

            if (!IsShopItem(line.Prototype))
                continue;

            if (!_prototypes.TryIndex<EntityPrototype>(line.Prototype, out var proto))
                continue;

            var unit = CFCoins.EntityPrice(proto);
            if (unit <= 0)
                continue;

            var count = Math.Clamp(line.Quantity, 1, remaining);
            total += unit * count;
            plan.Add((line.Prototype, count));
            remaining -= count;
        }

        if (plan.Count == 0)
        {
            Dirty(user, purse);
            return;
        }

        if (purse.Copper < total)
        {
            Dirty(user, purse);
            _popup.PopupEntity(Loc.GetString("cf-shop-short"), user, user);
            return;
        }

        var given = 0;
        foreach (var (prototype, count) in plan)
        {
            for (var i = 0; i < count; i++)
                GiveOne(user, prototype);

            given += count;
        }

        purse.Copper -= total;
        Dirty(user, purse);
        if (given > 0)
            _popup.PopupEntity(Loc.GetString("cf-shop-bought", ("count", given)), user, user);
    }

    private bool IsShopItem(string id)
    {
        if (id.Length == 0 || !id.StartsWith("CF", StringComparison.Ordinal))
            return false;

        if (!_prototypes.TryIndex<EntityPrototype>(id, out var proto) || proto.Abstract)
            return false;

        return proto.TryGetComponent<ItemComponent>(out _);
    }

    private void GiveOne(EntityUid user, string prototype)
    {
        var spawned = Spawn(prototype, Transform(user).Coordinates);
        if (TryPutInHand(user, spawned))
            return;

        if (TryInsertBag(user, spawned))
            return;

        var offset = DropOffsets[_drop % DropOffsets.Length];
        _drop++;
        _transform.SetCoordinates(spawned, Transform(user).Coordinates.Offset(offset));
    }

    private bool TryPutInHand(EntityUid user, EntityUid item)
    {
        if (!TryComp(user, out HandsComponent? hands))
            return false;

        foreach (var hand in _hands.EnumerateHands((user, hands)))
        {
            if (_hands.TryGetHeldItem((user, hands), hand, out _))
                continue;

            if (_hands.TryPickup(user, item, hand, checkActionBlocker: false, handsComp: hands))
                return true;
        }

        return false;
    }

    private bool TryInsertBag(EntityUid user, EntityUid item)
    {
        if (!_inventory.TryGetSlotEntity(user, BackSlot, out var bag) || bag == null)
            return false;

        if (!TryComp(bag.Value, out StorageComponent? storage))
            return false;

        if (!_storage.CanInsert(bag.Value, item, out _, storage))
            return false;

        return _storage.Insert(bag.Value, item, out _, user, storage, playSound: false);
    }
}
