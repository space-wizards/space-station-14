using System.Linq;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.HijackBeacon;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Cargo;

public abstract partial class SharedCargoSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationBankAccountComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HijackBeaconSuccessEvent>(OnHijackSuccess);
    }

    private void OnMapInit(Entity<StationBankAccountComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextIncomeTime = Timing.CurTime + ent.Comp.IncomeDelay;
        Dirty(ent);
    }

    private void OnHijackSuccess(ref HijackBeaconSuccessEvent args)
    {
        var stationQuery = EntityQueryEnumerator<StationBankAccountComponent>();
        while (stationQuery.MoveNext(out var uid, out var comp))
        {
            foreach (var (account, cash) in comp.Accounts)
            {
                comp.Accounts[account] = cash - args.Fine;
                args.Total += args.Fine;
            }

            var ev = new BankBalanceUpdatedEvent(uid, comp.Accounts);
            RaiseLocalEvent(uid, ref ev, true);
            Dirty(uid, comp);
        }
    }

    /// <summary>
    /// For a given station, retrieves the balance in a specific account.
    /// </summary>
    public int GetBalanceFromAccount(Entity<StationBankAccountComponent?> station, ProtoId<CargoAccountPrototype> account)
    {
        if (!Resolve(station, ref station.Comp))
            return 0;

        return station.Comp.Accounts.GetValueOrDefault(account);
    }

    /// <summary>
    /// For a station, creates a distribution between one the bank's account and the other accounts.
    /// The primary account receives the majority percentage listed on the bank account, with the remaining
    /// funds distributed to all accounts based on <see cref="StationBankAccountComponent.RevenueDistribution"/>
    /// </summary>
    public Dictionary<ProtoId<CargoAccountPrototype>, double> CreateAccountDistribution(Entity<StationBankAccountComponent> stationBank)
    {
        var distribution = new Dictionary<ProtoId<CargoAccountPrototype>, double>
        {
            { stationBank.Comp.PrimaryAccount, stationBank.Comp.PrimaryCut }
        };
        var remaining = 1.0 - stationBank.Comp.PrimaryCut;

        foreach (var (account, percentage) in stationBank.Comp.RevenueDistribution)
        {
            var existing = distribution.GetOrNew(account);
            distribution[account] = existing + remaining * percentage;
        }
        return distribution;
    }

    /// <summary>
    /// Returns information about the given bank account.
    /// </summary>
    /// <param name="station">Station to get bank account info from.</param>
    /// <param name="accountPrototypeId">Bank account prototype ID to get info for.</param>
    /// <param name="money">The amount of money in the account</param>
    /// <returns>Whether or not the bank account exists.</returns>
    public bool TryGetAccount(Entity<StationBankAccountComponent?> station, ProtoId<CargoAccountPrototype> accountPrototypeId, out int money)
    {
        money = 0;

        if (!Resolve(station, ref station.Comp))
            return false;

        return station.Comp.Accounts.TryGetValue(accountPrototypeId, out money);
    }

    /// <summary>
    /// Returns a readonly dictionary of all accounts and their money info.
    /// </summary>
    /// <param name="station">Station to get bank account info from.</param>
    /// <returns>Whether or not the bank account exists.</returns>
    public IReadOnlyDictionary<ProtoId<CargoAccountPrototype>, int> GetAccounts(Entity<StationBankAccountComponent?> station)
    {
        if (!Resolve(station, ref station.Comp))
            return new Dictionary<ProtoId<CargoAccountPrototype>, int>();

        return station.Comp.Accounts;
    }

    /// <summary>
    /// Attempts to adjust the money of a certain bank account.
    /// </summary>
    /// <param name="station">Station where the bank account is from</param>
    /// <param name="accountPrototypeId">the id of the bank account</param>
    /// <param name="money">how much money to set the account to</param>
    /// <param name="createAccount">Whether or not it should create the account if it doesn't exist.</param>
    /// <param name="dirty">Whether to mark the bank account component as dirty.</param>
    /// <returns>Whether or not setting the value succeeded.</returns>
    public bool TryAdjustBankAccount(
        Entity<StationBankAccountComponent?> station,
        ProtoId<CargoAccountPrototype> accountPrototypeId,
        int money,
        bool createAccount = false,
        bool dirty = true)
    {
        if (!Resolve(station, ref station.Comp))
            return false;

        var accounts = station.Comp.Accounts;

        if (!accounts.ContainsKey(accountPrototypeId) && !createAccount)
            return false;

        accounts[accountPrototypeId] += money;
        var ev = new BankBalanceUpdatedEvent(station, station.Comp.Accounts);
        RaiseLocalEvent(station, ref ev, true);

        if (!dirty)
            return true;

        Dirty(station);
        return true;
    }

    /// <summary>
    /// Attempts to set the money of a certain bank account.
    /// </summary>
    /// <param name="station">Station where the bank account is from</param>
    /// <param name="accountPrototypeId">the id of the bank account</param>
    /// <param name="money">how much money to set the account to</param>
    /// <param name="createAccount">Whether or not it should create the account if it doesn't exist.</param>
    /// <param name="dirty">Whether to mark the bank account component as dirty.</param>
    /// <returns>Whether or not setting the value succeeded.</returns>
    public bool TrySetBankAccount(
        Entity<StationBankAccountComponent?> station,
        ProtoId<CargoAccountPrototype> accountPrototypeId,
        int money,
        bool createAccount = false,
        bool dirty = true)
    {
        if (!Resolve(station, ref station.Comp))
            return false;

        var accounts = station.Comp.Accounts;

        if (!accounts.ContainsKey(accountPrototypeId) && !createAccount)
            return false;

        accounts[accountPrototypeId] = money;
        var ev = new BankBalanceUpdatedEvent(station, station.Comp.Accounts);
        RaiseLocalEvent(station, ref ev, true);

        if (!dirty)
            return true;

        Dirty(station);
        return true;
    }

    public void UpdateBankAccount(
        Entity<StationBankAccountComponent?> ent,
        int balanceAdded,
        ProtoId<CargoAccountPrototype> account,
        bool dirty = true)
    {
        UpdateBankAccount(
            ent,
            balanceAdded,
            new Dictionary<ProtoId<CargoAccountPrototype>, double> { {account, 1} },
            dirty: dirty);
    }

    /// <summary>
    /// Adds or removes funds from the <see cref="StationBankAccountComponent"/>.
    /// </summary>
    /// <param name="ent">The station.</param>
    /// <param name="balanceAdded">The amount of funds to add or remove.</param>
    /// <param name="accountDistribution">The distribution between individual <see cref="CargoAccountPrototype"/>.</param>
    /// <param name="dirty">Whether to mark the bank account component as dirty.</param>
    [PublicAPI]
    public void UpdateBankAccount(
        Entity<StationBankAccountComponent?> ent,
        int balanceAdded,
        Dictionary<ProtoId<CargoAccountPrototype>, double> accountDistribution,
        bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        foreach (var (account, percent) in accountDistribution)
        {
            var accountBalancedAdded = (int) Math.Round(percent * balanceAdded);
            ent.Comp.Accounts[account] += accountBalancedAdded;
        }

        var ev = new BankBalanceUpdatedEvent(ent, ent.Comp.Accounts);
        RaiseLocalEvent(ent, ref ev, true);

        if (!dirty)
            return;

        Dirty(ent);
    }
    public int GetBasketCost(List<CargoOrderItemData> basket)
    {
        return basket.Sum(item =>
        {
            var proto = ProtoMan.Index<CargoProductPrototype>(item.Product);
            return proto.Cost * item.Quantity;
        });
    }

    public int GetContainersCost(List<CargoOrderContainerData> containers)
    {
        return containers.Sum(container => container.Cost);
    }
    public int GetBasketTotalCost(List<CargoOrderItemData> basket)
    {
        var cost = GetBasketCost(basket);
        var containers = PackBasketIntoContainers(ref basket);
        cost += GetContainersCost(containers);
        return cost;
    }
    public List<CargoOrderContainerData> PackBasketIntoContainers(ref List<CargoOrderItemData> basket)
    {
        var containers = new List<CargoOrderContainerData>();

        for (int j = 0; j < basket.Count; j++)
        {
            var item = basket[j];
            if (!ShouldOrderItem(item))
                continue;

            if (!ProtoMan.TryIndex<CargoProductPrototype>(item.Product, out var productProto))
                continue;

            if (!item.WithContainer || productProto.Container == null)
            {
                for (int i = 0; i < item.Quantity - item.NumOrdered; i++)
                {
                    containers.Add(new CargoOrderContainerData("", "", item));
                }
                continue;
            }

            if (!ProtoMan.TryIndex<CargoCratePrototype>(productProto.Container, out var crate))
                continue;

            PackItemIntoCrates(ref item, crate, containers);
            basket[j] = item;
        }
        return containers;
    }

    private bool ShouldOrderItem(CargoOrderItemData item)
    {
        return item.ToBeOrdered && item.NumOrdered < item.Quantity;
    }

    private void PackItemIntoCrates(
        ref CargoOrderItemData item,
        CargoCratePrototype crate,
        List<CargoOrderContainerData> containers
    )
    {
        var remaining = GetItemEntityCount(item) * (item.Quantity - item.NumOrdered);

        // Try to fit into an existing container with space
        foreach (var container in containers)
        {
            if (remaining <= 0)
                break;

            if (!CanFitInContainer(container, item, crate))
                continue;

            var fitting =
                Math.Min(remaining, container.MaxItems - GetContainerItemCount(container)) / GetItemEntityCount(item);
            container.Products.Add(new CargoOrderContainerSlot(item, fitting));
            remaining -= fitting * GetItemEntityCount(item);
        }

        // Overflow into new containers
        while (remaining > 0)
        {
            var batch = Math.Min(remaining, crate.MaxItems) / GetItemEntityCount(item);
            var container = new CargoOrderContainerData(
                crate.Entity,
                crate.ContainerId,
                crateRequired: crate.Required,
                maxItems: crate.MaxItems,
                cost: crate.Cost
            );
            container.Products.Add(new CargoOrderContainerSlot(item, batch));
            containers.Add(container);
            remaining -= batch * GetItemEntityCount(item);
        }
        var parcel = (ProtoId<CargoCratePrototype>)"WrappedParcel";
        foreach (var container in containers)
        {
            if (
                !container.IsSingleProduct
                && GetContainerItemCount(container) == 1
                && !container.CrateRequired
                && ProtoMan.Resolve<CargoCratePrototype>(parcel, out var parcelProto))
            {
                container.Container = parcelProto.Entity;
                container.ContainerID = parcelProto.ContainerId;
                container.Cost = parcelProto.Cost;
                container.MaxItems = parcelProto.MaxItems;
            }
        }
    }

    private bool CanFitInContainer(
        CargoOrderContainerData container,
        CargoOrderItemData item,
        CargoCratePrototype crate
    )
    {
        if (!ProtoMan.TryIndex<CargoProductPrototype>(item.Product, out var proto))
            return false;
        return container.Container != ""
            && (EntProtoId)container.Container == crate.Entity
            && GetContainerItemCount(container) <= container.MaxItems - item.Quantity * GetItemEntityCount(item)
            && container.CrateRequired == crate.Required;
    }

    public int GetContainerItemCount(CargoOrderContainerData container)
    {
        return container.Products.Sum(item => item.Quantity * GetItemEntityCount(item.Source));
    }

    public int GetItemEntityCount(CargoOrderItemData item)
    {
        if (!ProtoMan.TryIndex<CargoProductPrototype>(item.Product, out var proto))
        {
            return 1;
        }
        return proto.SpawnList.Count;
    }
}

[NetSerializable, Serializable]
public enum CargoConsoleUiKey : byte
{
    Orders,
    Bounty,
    Shuttle,
    Telepad
}

[NetSerializable, Serializable]
public enum CargoPalletConsoleUiKey : byte
{
    Sale
}

[Serializable, NetSerializable]
public enum CargoTelepadState : byte
{
    Unpowered,
    Idle,
    Teleporting,
};

[Serializable, NetSerializable]
public enum CargoTelepadVisuals : byte
{
    State,
};
