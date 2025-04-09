using System.Linq;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo;

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

public abstract class SharedCargoSystem : EntitySystem
{
    public int GetBalanceFromAccount(Entity<StationBankAccountComponent?> station, ProtoId<CargoAccountPrototype> account)
    {
        if (!Resolve(station, ref station.Comp))
            return 0;

        return station.Comp.Accounts.GetValueOrDefault(account);
    }

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
