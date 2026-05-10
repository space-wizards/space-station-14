using Content.Server._FinalStand.Economy;
using Content.Server.Popups;
using Content.Shared._FinalStand.Shop;
using Content.Shared.Examine;
using Content.Shared.Mind;

namespace Content.Server._FinalStand.Shop;

public sealed class FSShopWeaponSystem : EntitySystem
{
    [Dependency] private readonly FSPlayerWalletSystem _wallet = default!;
    [Dependency] private readonly FSPlayerUpgradesSystem _upgrades = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSShopWeaponComponent, ExaminedEvent>(OnExamined);
        Subs.BuiEvents<FSShopWeaponComponent>(FSShopWeaponUiKey.Key, subs =>
        {
            subs.Event<FSShopBuyMessage>(OnBuyMessage);
            subs.Event<FSShopUpgradeMessage>(OnUpgradeMessage);
        });
    }

    private void OnExamined(EntityUid uid, FSShopWeaponComponent comp, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("shop-weapon-examine-price", ("price", comp.Price)));
    }

    private void OnBuyMessage(EntityUid uid, FSShopWeaponComponent comp, FSShopBuyMessage args)
    {
        var player = args.Actor;
        if (!player.IsValid())
            return;

        if (!_mind.TryGetMind(player, out var mindId, out _))
            return;

        if (!_wallet.TryDeductCredits(mindId, comp.Price))
        {
            _popup.PopupEntity(Loc.GetString("shop-weapon-insufficient-funds"), uid, player);
            return;
        }

        var weapon = Spawn(comp.WeaponProtoId, Transform(player).Coordinates);
        _upgrades.ApplyUpgrades(weapon, mindId, comp.Upgrades);
        _popup.PopupEntity(Loc.GetString("shop-weapon-purchased"), uid, player);
    }

    private void OnUpgradeMessage(EntityUid uid, FSShopWeaponComponent comp, FSShopUpgradeMessage args)
    {
        var player = args.Actor;
        if (!player.IsValid())
            return;

        if (!_mind.TryGetMind(player, out var mindId, out _))
            return;

        WeaponUpgradeDef? def = null;
        foreach (var upgrade in comp.Upgrades)
        {
            if (upgrade.Id == args.UpgradeId) { def = upgrade; break; }
        }
        if (def == null)
            return;

        var currentLevel = _upgrades.GetLevel(mindId, def.Id);
        if (currentLevel >= def.MaxLevel)
        {
            _popup.PopupEntity(Loc.GetString("shop-upgrade-max-level"), uid, player);
            return;
        }

        var cost = def.BaseCost * (currentLevel + 1);
        if (!_wallet.TryDeductCredits(mindId, cost))
        {
            _popup.PopupEntity(Loc.GetString("shop-weapon-insufficient-funds"), uid, player);
            return;
        }

        _upgrades.TryPurchase(mindId, def.Id, def.MaxLevel, out _);
        _upgrades.NotifyClient(mindId);
        _popup.PopupEntity(Loc.GetString("shop-upgrade-purchased", ("name", def.Name)), uid, player);
    }
}
