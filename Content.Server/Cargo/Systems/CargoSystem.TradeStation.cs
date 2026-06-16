using System.Linq;
using Content.Server.Cargo.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    /*
     * Handles automated trade station / trade mechanics.
     */

    private static readonly SoundPathSpecifier ApproveSound = new("/Audio/Effects/Cargo/ping.ogg");
    private bool _lockboxCutEnabled;

    private void InitializeShuttle()
    {
        SubscribeLocalEvent<TradeStationComponent, GridSplitEvent>(OnTradeSplit);

        SubscribeLocalEvent<CargoPalletConsoleComponent, CargoPalletSellMessage>(OnPalletSale);
        SubscribeLocalEvent<CargoPalletConsoleComponent, CargoPalletAppraiseMessage>(OnPalletAppraise);
        SubscribeLocalEvent<CargoPalletConsoleComponent, BoundUIOpenedEvent>(OnPalletUIOpen);

        _cfg.OnValueChanged(
            CCVars.LockboxCutEnabled,
            (enabled) =>
            {
                _lockboxCutEnabled = enabled;
            },
            true
        );
    }

    #region Console
    private void UpdatePalletConsoleInterface(EntityUid uid)
    {
        if (Transform(uid).GridUid is not { } gridUid)
        {
            _uiSystem.SetUiState(uid, CargoPalletConsoleUiKey.Sale, new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }
        GetPalletGoods(gridUid, out var goods);
        _uiSystem.SetUiState(
            uid,
            CargoPalletConsoleUiKey.Sale,
            new CargoPalletConsoleInterfaceState((int)goods.Sum(t => t.price), goods.Count, true)
        );
    }

    private void OnPalletUIOpen(EntityUid uid, CargoPalletConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdatePalletConsoleInterface(uid);
    }

    /// <summary>
    /// Ok so this is just the same thing as opening the UI, its a refresh button.
    /// I know this would probably feel better if it were like predicted and dynamic as pallet contents change
    /// However.
    /// I dont want it to explode if cargo uses a conveyor to move 8000 pineapple slices or whatever, they are
    /// known for their entity spam i wouldnt put it past them
    /// </summary>
    private void OnPalletAppraise(EntityUid uid, CargoPalletConsoleComponent component, CargoPalletAppraiseMessage args)
    {
        UpdatePalletConsoleInterface(uid);
    }

    private void OnTradeSplit(EntityUid uid, TradeStationComponent component, ref GridSplitEvent args)
    {
        // If the trade station gets bombed it's still a trade station.
        foreach (var gridUid in args.NewGrids)
        {
            EnsureComp<TradeStationComponent>(gridUid);
        }
    }

    /// <summary>
    /// Gets all sell pallets on a grid
    /// </summary>
    /// <param name="gridUid"> Grid to find pallets on</param>
    /// <returns>Iterator of pallet (Uid, Transform)</returns>
    public IEnumerable<(Entity<CargoPalletComponent> Entity, TransformComponent PalletXform)> GetCargoPallets(
        EntityUid gridUid,
        BuySellType requestType = BuySellType.All
    )
    {
        var query = EntityQueryEnumerator<CargoPalletComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var compXform))
        {
            if ((requestType & comp.PalletType) == 0 || compXform.ParentUid != gridUid || !compXform.Anchored)
                continue;
            yield return ((uid, comp), compXform);
        }
    }

    /// <summary>
    /// Gets all free buy pallets on a grid
    /// </summary>
    /// <param name="gridUid"> Grid to find pallets on</param>
    /// <returns>Iterator of pallet (Uid, Transform)</returns>
    public IEnumerable<(Entity<CargoPalletComponent> Entity, TransformComponent Transform)> GetFreeCargoPallets(
        EntityUid gridUid,
        BuySellType requestType = BuySellType.Buy
    )
    {
        foreach (var pallet in GetCargoPallets(gridUid, requestType))
        {
            var aabb = _lookup.GetAABBNoContainer(
                pallet.Entity,
                pallet.PalletXform.LocalPosition,
                pallet.PalletXform.LocalRotation
            );
            if (_lookup.AnyLocalEntitiesIntersecting(gridUid, aabb, LookupFlags.Dynamic))
                continue;

            yield return (pallet.Entity, pallet.PalletXform);
        }
    }

    public IEnumerable<EntityUid> GetEntitiesOnCargoPallets(EntityUid gridUid)
    {
        var entities = new HashSet<EntityUid>();
        foreach (var pallet in GetCargoPallets(gridUid, BuySellType.Buy))
        {
            var aabb = _lookup.GetAABBNoContainer(
                pallet.Entity,
                pallet.PalletXform.LocalPosition,
                pallet.PalletXform.LocalRotation
            );
            _lookup.GetLocalEntitiesIntersecting(gridUid, aabb, entities, LookupFlags.Dynamic);
        }
        return entities.Distinct();
    }

    #endregion

    #region Station

    private bool SellPallets(EntityUid gridUid, EntityUid station, HashSet<EntityUid> toSell)
    {
        if (toSell.Count == 0)
            return false;

        var ev = new EntitySoldEvent(toSell, station);
        RaiseLocalEvent(ref ev);

        foreach (var ent in toSell)
        {
            Del(ent);
        }

        return true;
    }

    public void GetPalletGoods(
        EntityUid gridUid,
        out HashSet<(EntityUid ent, OverrideSellComponent? overrideSellComponent, double price)> goods
    )
    {
        goods = new HashSet<(EntityUid, OverrideSellComponent?, double)>();

        foreach (var ent in GetEntitiesOnCargoPallets(gridUid))
        {
            // Don't sell:
            // - anything already being sold
            // - anything anchored (e.g. light fixtures)
            // - anything blacklisted (e.g. players).
            if (Transform(ent).Anchored || !CanSell(ent))
                continue;

            var price = _pricing.GetPrice(ent);
            if (price == 0)
                continue;
            goods.Add((ent, CompOrNull<OverrideSellComponent>(ent), price));
        }
    }

    public bool CanSell(EntityUid uid)
    {
        if (_mobStateQuery.HasComponent(uid) || _cargoSellBlacklistQuery.HasComponent(uid))
            return false;

        var complete = IsBountyComplete(uid, out var bountyEntities);

        // Recursively check for mobs at any point.
        var xform = Transform(uid);
        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (complete && bountyEntities.Contains(child))
                continue;

            if (!CanSell(child))
                return false;
        }

        return true;
    }

    private void OnPalletSale(EntityUid uid, CargoPalletConsoleComponent component, CargoPalletSellMessage args)
    {
        var xform = Transform(uid);

        if (
            _station.GetOwningStation(uid) is not { } station
            || !TryComp<StationBankAccountComponent>(station, out var bankAccount)
        )
        {
            return;
        }

        if (xform.GridUid is not { } gridUid)
        {
            _uiSystem.SetUiState(uid, CargoPalletConsoleUiKey.Sale, new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        GetPalletGoods(gridUid, out var goods);

        if (!SellPallets(gridUid, station, goods.Select(x => x.ent).ToHashSet()))
            return;

        var baseDistribution = CreateAccountDistribution((station, bankAccount));
        foreach (var (_, sellComponent, value) in goods)
        {
            Dictionary<ProtoId<CargoAccountPrototype>, double> distribution;
            if (sellComponent != null)
            {
                var cut = _lockboxCutEnabled ? bankAccount.LockboxCut : bankAccount.PrimaryCut;
                distribution = new Dictionary<ProtoId<CargoAccountPrototype>, double>
                {
                    { sellComponent.OverrideAccount, cut },
                    { bankAccount.PrimaryAccount, 1.0 - cut },
                };
            }
            else
            {
                distribution = baseDistribution;
            }

            UpdateBankAccount((station, bankAccount), (int)Math.Round(value), distribution, false);
        }

        Dirty(station, bankAccount);
        _audio.PlayPvs(ApproveSound, uid);
        UpdatePalletConsoleInterface(uid);
    }

    #endregion
}

/// <summary>
/// Event broadcast raised by-ref before it is sold and
/// deleted but after the price has been calculated.
/// </summary>
[ByRefEvent]
public readonly record struct EntitySoldEvent(HashSet<EntityUid> Sold, EntityUid Station);
