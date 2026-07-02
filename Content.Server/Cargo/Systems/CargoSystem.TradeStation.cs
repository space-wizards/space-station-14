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

        _cfg.OnValueChanged(CCVars.LockboxCutEnabled, (enabled) => { _lockboxCutEnabled = enabled; }, true);
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

    #endregion

    #region Pallets

    /// <summary>
    /// Returns all cargo pallets on a grid, filtered by buy/sell type.
    /// </summary>
    /// <param name="gridUid">The grid to search for pallets.</param>
    /// <param name="requestType">Which pallet types to include. Defaults to <see cref="BuySellType.All"/>.</param>
    /// <returns>Each pallet entity with its <see cref="TransformComponent"/>.</returns>
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
    /// Returns all unoccupied cargo pallets on a grid, filtered by buy/sell type.
    /// A pallet is considered free if no dynamic entities are intersecting it.
    /// </summary>
    /// <param name="gridUid">The grid to search for pallets.</param>
    /// <param name="requestType">Which pallet types to include. Defaults to <see cref="BuySellType.Buy"/>.</param>
    /// <returns>Each free pallet entity with its <see cref="TransformComponent"/>.</returns>
    public IEnumerable<(Entity<CargoPalletComponent> Entity, TransformComponent Transform)> GetFreeCargoPallets(
        EntityUid gridUid,
        BuySellType requestType = BuySellType.Buy
    )
    {
        foreach (var pallet in GetCargoPallets(gridUid, requestType))
        {
            if (IsPalletOccupied(pallet))
                continue;

            yield return (pallet.Entity, pallet.PalletXform);
        }
    }

    /// <summary>
    /// Is the given pallet free of dynamic entities
    /// </summary>
    /// <param name="pallet"> The pallet to check. </param>
    /// <returns> <c>true</c> if the pallet has no dynamic entities on it; otherwise <c>false</c>. </returns>
    public bool IsPalletOccupied((Entity<CargoPalletComponent> Entity, TransformComponent PalletXform) pallet)
    {
        var aabb = _lookup.GetAABBNoContainer(
            pallet.Entity,
            pallet.PalletXform.LocalPosition,
            pallet.PalletXform.LocalRotation
        );
        return _lookup.AnyLocalEntitiesIntersecting(pallet.PalletXform.ParentUid, aabb, LookupFlags.Dynamic);
    }

    /// <summary>
    /// Returns all dynamic entities currently sitting on pallets on a grid, filtered by buy/sell type.
    /// </summary>
    /// <param name="gridUid">The grid to search.</param>
    /// <param name="requestType">Which pallet types to include. Defaults to <see cref="BuySellType.Sell"/>.</param>
    /// <param name="pallets"> Specific pallets to check. If set then <see cref="requestType"/> is ignored. </param>
    /// <returns>Distinct set of entity UIDs found on pallets.</returns>
    public IEnumerable<EntityUid> GetEntitiesOnCargoPallets(
        EntityUid gridUid,
        BuySellType requestType = BuySellType.Sell,
        IEnumerable<(Entity<CargoPalletComponent> Entity, TransformComponent PalletXform)>? pallets = null
    )
    {
        var entities = new HashSet<EntityUid>();
        foreach (var pallet in pallets ?? GetCargoPallets(gridUid, requestType))
        {
            var aabb = _lookup.GetAABBNoContainer(
                pallet.Entity,
                pallet.PalletXform.LocalPosition,
                pallet.PalletXform.LocalRotation
            );
            _lookup.GetLocalEntitiesIntersecting(gridUid, aabb, entities, LookupFlags.Dynamic | LookupFlags.Sundries);
        }
        return entities;
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

    /// <summary>
    /// Collects all sellable goods from cargo pallets on a grid, along with their prices
    /// and any sell overrides. Excludes anchored entities, mobs, blacklisted entities,
    /// and anything with a price of zero.
    /// </summary>
    /// <param name="gridUid">The grid to appraise.</param>
    /// <param name="goods">
    /// Output set of <c>(entity, overrideSellComponent, price)</c> tuples for each sellable item.
    /// </param>
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

    /// <summary>
    /// Determines whether an entity is eligible to be sold.
    /// An entity cannot be sold if it is a mob, is cargo-blacklisted,
    /// or contains any such entities recursively in its children.
    /// </summary>
    /// <param name="uid">The entity to check.</param>
    /// <returns><c>true</c> if the entity and all its children are sellable; otherwise <c>false</c>.</returns>
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
