using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Cargo;

public abstract class SharedCargoSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationBankAccountComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<StationBankAccountComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextIncomeTime = Timing.CurTime + ent.Comp.IncomeDelay;
        Dirty(ent);
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
    /// <param name="money">The ammount of money in the account</param>
    /// <returns>Whether or not the bank account exists.</returns>
    public bool TryGetAccount(Entity<StationBankAccountComponent> station, ProtoId<CargoAccountPrototype> accountPrototypeId, out int money)
    {
        return station.Comp.Accounts.TryGetValue(accountPrototypeId, out money);
    }

    /// <summary>
    /// Returns a readonly dictionary of all accounts and their money info.
    /// </summary>
    /// <param name="station">Station to get bank account info from.</param>
    /// <returns>Whether or not the bank account exists.</returns>
    public IReadOnlyDictionary<ProtoId<CargoAccountPrototype>, int> GetAccounts(Entity<StationBankAccountComponent> station)
    {
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
        Entity<StationBankAccountComponent> station,
        ProtoId<CargoAccountPrototype> accountPrototypeId,
        int money,
        bool createAccount = false,
        bool dirty = true)
    {
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
        Entity<StationBankAccountComponent> station,
        ProtoId<CargoAccountPrototype> accountPrototypeId,
        int money,
        bool createAccount = false,
        bool dirty = true)
    {
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
