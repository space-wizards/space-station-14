using System.Linq;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

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
    /// For a station, creates a distribution between one "primary" account and the other accounts.
    /// The primary account receives the majority cut specified, with the remaining accounts getting cuts
    /// distributed through the remaining amount, based on <see cref="StationBankAccountComponent.RevenueDistribution"/>
    /// </summary>
    public Dictionary<ProtoId<CargoAccountPrototype>, double> CreateAccountDistribution(
        ProtoId<CargoAccountPrototype> primary,
        StationBankAccountComponent stationBank,
        double primaryCut = 1.0)
    {
        var distribution = new Dictionary<ProtoId<CargoAccountPrototype>, double>
        {
            { primary, primaryCut }
        };
        var remaining = 1.0 - primaryCut;

        var allAccountPercentages = new Dictionary<ProtoId<CargoAccountPrototype>, double>(stationBank.RevenueDistribution);
        allAccountPercentages.Remove(primary);
        var weightsSum = allAccountPercentages.Values.Sum();

        foreach (var (account, percentage) in allAccountPercentages)
        {
            var adjustedPercentage = percentage / weightsSum;

            distribution.Add(account, remaining * adjustedPercentage);
        }
        return distribution;
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
