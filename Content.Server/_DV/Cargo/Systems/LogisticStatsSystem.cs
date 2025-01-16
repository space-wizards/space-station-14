using Content.Server._DV.Cargo.Components;
using Content.Shared.Cargo;
using JetBrains.Annotations;

namespace Content.Server._DV.Cargo.Systems;

public sealed class LogisticStatsSystem : SharedCargoSystem
{

    [PublicAPI]
    public void AddOpenedMailEarnings(Entity<StationLogisticStatsComponent?> ent, int earnedMoney)
    {
        if (ent.Comp != null)
        {
            ent.Comp.Metrics = ent.Comp.Metrics with
            {
                Earnings = ent.Comp.Metrics.Earnings + earnedMoney,
                OpenedCount = ent.Comp.Metrics.OpenedCount + 1
            };
        }

        UpdateLogisticsStats(ent);
    }

    [PublicAPI]
    public void AddExpiredMailLosses(EntityUid uid, StationLogisticStatsComponent component, int lostMoney)
    {
        component.Metrics = component.Metrics with
        {
            ExpiredLosses = component.Metrics.ExpiredLosses + lostMoney,
            ExpiredCount = component.Metrics.ExpiredCount + 1
        };
        UpdateLogisticsStats(uid);
    }

    [PublicAPI]
    public void AddDamagedMailLosses(EntityUid uid, StationLogisticStatsComponent component, int lostMoney)
    {
        component.Metrics = component.Metrics with
        {
            DamagedLosses = component.Metrics.DamagedLosses + lostMoney,
            DamagedCount = component.Metrics.DamagedCount + 1
        };
        UpdateLogisticsStats(uid);
    }

    [PublicAPI]
    public void AddTamperedMailLosses(EntityUid uid, StationLogisticStatsComponent component, int lostMoney)
    {
        component.Metrics = component.Metrics with
        {
            TamperedLosses = component.Metrics.TamperedLosses + lostMoney,
            TamperedCount = component.Metrics.TamperedCount + 1
        };
        UpdateLogisticsStats(uid);
    }

    private void UpdateLogisticsStats(EntityUid uid)
    {
        var ev = new LogisticStatsUpdatedEvent(uid);
        RaiseLocalEvent(ev);
    }
}

public sealed class LogisticStatsUpdatedEvent : EntityEventArgs
{
    public EntityUid Station;
    public LogisticStatsUpdatedEvent(EntityUid station)
    {
        Station = station;
    }
}
