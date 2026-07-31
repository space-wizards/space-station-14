using Content.Server.Cargo.Systems;
using Robust.Shared.Timing;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Cargo.Prototypes;

namespace Content.Server.DeadSpace.GameRules;

public sealed class CashCollectionSystem : GameRuleSystem<CashCollectionComponent>
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // DS14-start
        if (GameTicker.RunLevel == GameRunLevel.PostRound)
            return;
        // DS14-end

        var query = EntityQueryEnumerator<CashCollectionComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var cashCollection, out var gameRule))
        {
            if (!_gameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            cashCollection.CheckAccumulator += frameTime;
            if (cashCollection.CheckAccumulator < cashCollection.CheckInterval)
                continue;

            cashCollection.CheckAccumulator = 0f;
            CheckAccounts(cashCollection);
        }
    }

    private void CheckAccounts(CashCollectionComponent cashCollection)
    {
        if (_timing.CurTime < cashCollection.NextAllowedSpawn)
            return;

        foreach (var station in _station.GetStations())
        {
            if (!TryComp<StationBankAccountComponent>(station, out var bank))
                continue;

            var thresholdExceeded = false;
            foreach (var account in bank.Accounts)
            {
                if (_cargo.GetBalanceFromAccount(station, account.Key) > cashCollection.Threshold)
                {
                    if (account.Key == "Taipan")
                        continue;

                    thresholdExceeded = true;
                    break;
                }
            }

            if (!thresholdExceeded)
                continue;

            if (_gameTicker.IsGameRuleActive(cashCollection.CashCollectionRule))
                return;

            cashCollection.NextAllowedSpawn = _timing.CurTime + cashCollection.ShuttleCooldown;

            _gameTicker.AddGameRule(cashCollection.CashCollectionRule);
            return;
        }
    }
}
