using Content.Shared.Cargo.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
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
    /// A dictionary corresponding to the money held by each cargo account.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CargoAccountPrototype>, int> Accounts = new()
    {
        { "Cargo",       2000 },
        { "Engineering", 1000 },
        { "Medical",     1000 },
        { "Science",     1000 },
        { "Security",    1000 },
        { "Service",     1000 },
    };

    /// <summary>
    /// A baseline distribution used for income and dispersing leftovers after sale.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CargoAccountPrototype>, double> RevenueDistribution = new()
    {
        { "Cargo",       0.00 },
        { "Engineering", 0.20 },
        { "Medical",     0.20 },
        { "Science",     0.20 },
        { "Security",    0.20 },
        { "Service",     0.20 },
    };

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
public readonly record struct BankBalanceUpdatedEvent(EntityUid Station, Dictionary<ProtoId<CargoAccountPrototype>, int> Balance);
