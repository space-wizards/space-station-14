using Content.Shared.Cargo.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Added to the abstract representation of a station to track its money.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCargoSystem)), AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class StationBankAccountComponent : Component
{
    /// <summary>
    /// The account that receives funds by default
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<CargoAccountPrototype> PrimaryAccount = "Cargo";

    /// <summary>
    /// When giving funds to a particular account, the proportion of funds they should receive compared to remaining accounts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double PrimaryCut = 0.50;

    /// <summary>
    /// When giving funds to a particular account from an override sell, the proportion of funds they should receive compared to remaining accounts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double LockboxCut = 0.75;

    /// <summary>
    /// A dictionary corresponding to the data of each cargo account.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CargoAccountPrototype>, CargoAccountData> Accounts;

    /// <summary>
    /// How much the bank balance goes up per second, every Delay period. Rounded down when multiplied.
    /// </summary>
    [DataField]
    public int IncreasePerSecond = 2;

    /// <summary>
    /// The time at which the station will receive its next deposit of passive income
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextIncomeTime;

    /// <summary>
    /// How much time to wait (in seconds) before increasing bank accounts balance.
    /// </summary>
    [DataField]
    public TimeSpan IncomeDelay = TimeSpan.FromSeconds(50);
}

/// <summary>
/// Broadcast and raised on station ent whenever its balance is updated.
/// </summary>
[ByRefEvent]
public readonly record struct BankBalanceUpdatedEvent(EntityUid Station, Dictionary<ProtoId<CargoAccountPrototype>, CargoAccountData> Accounts);

/// <summary>
/// Contains the data for each CargoAccountPrototype.
/// </summary>
[DataDefinition, NetSerializable, Serializable]
public sealed partial class CargoAccountData : IRobustCloneable<CargoAccountData>
{
    /// <summary>
    ///     The money held by the cargo account.
    /// </summary>
    [DataField]
    public int Balance;

    /// <summary>
    ///     The proportion used for income and dispersing leftovers after sale.
    /// </summary>
    [DataField]
    public double RevenueDistribution;

    public CargoAccountData(int balance, double revenueDistribution)
    {
        Balance = balance;
        RevenueDistribution = revenueDistribution;
    }

    public CargoAccountData(CargoAccountData data) : this(
        data.Balance,
        data.RevenueDistribution
        )
    {
    }

    public CargoAccountData Clone()
    {
        return new CargoAccountData(this);
    }
}
