using System.Diagnostics;
using Content.Server.Administration.Logs;
using Content.Server.RoundEnd;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class DynamicRuleSystem : GameRuleSystem<DynamicRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Added(EntityUid uid, DynamicRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        component.Budget = _random.Next(component.StartingBudgetMin, component.StartingBudgetMax);;
        component.NextRuleTime = Timing.CurTime + RobustRandom.Next(component.MinRuleInterval, component.MaxRuleInterval);
    }

    protected override void Started(EntityUid uid, DynamicRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        RunRule((uid, component, gameRule));
    }

    protected override void Ended(EntityUid uid, DynamicRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        foreach (var rule in component.Rules)
        {
            GameTicker.EndGameRule(rule);
        }
    }

    protected override void ActiveTick(EntityUid uid, DynamicRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (Timing.CurTime < component.NextRuleTime)
            return;

        component.NextRuleTime = Timing.CurTime + RobustRandom.Next(component.MinRuleInterval, component.MaxRuleInterval);

        // don't spawn antags during evac
        if (_roundEnd.IsRoundEndRequested())
            return;

        RunRule((uid, component, gameRule));
    }

    private IEnumerable<EntProtoId> GetRuleSpawns(Entity<DynamicRuleComponent> ent)
    {
        var ctx = new EntityTableContext(new Dictionary<string, object>
        {
            { HasBudgetCondition.BudgetContextKey, ent.Comp.Budget },
        });

        return _entityTable.GetSpawns(ent.Comp.Table, ctx: ctx);
    }

    private void RunRule(Entity<DynamicRuleComponent, GameRuleComponent> ent)
    {
        var duration = (Timing.CurTime - ent.Comp2.ActivatedAt).TotalSeconds;
        var budget = ent.Comp1.Budget + duration * ent.Comp1.BudgetPerSecond;

        foreach (var rule in GetRuleSpawns(ent))
        {
            var res = GameTicker.StartGameRule(rule, out var ruleUid);
            Debug.Assert(res);

            ent.Comp1.Rules.Add(ruleUid);

            if (TryComp<DynamicRuleCostComponent>(ruleUid, out var cost))
            {
                ent.Comp1.Budget -= cost.Cost;
                _adminLog.Add(LogType.EventRan, LogImpact.High, $"{ToPrettyString(ent)} ran rule {ToPrettyString(ruleUid)} with cost {cost.Cost} on budget {budget}.");
            }
            else
            {
                _adminLog.Add(LogType.EventRan, LogImpact.High, $"{ToPrettyString(ent)} ran rule {ToPrettyString(ruleUid)} which had no cost.");
            }
        }
    }

    #region Command Methods

    public List<EntityUid> GetDynamicRules()
    {
        var rules = new List<EntityUid>();
        var query = EntityQueryEnumerator<DynamicRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            if (!GameTicker.IsGameRuleActive(uid, comp))
                continue;
            rules.Add(uid);
        }

        return rules;
    }

    public float? GetRuleBudget(Entity<DynamicRuleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;

        return entity.Comp.Budget;
    }

    public float? AdjustBudget(Entity<DynamicRuleComponent?> entity, float amount)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;

        entity.Comp.Budget += amount;
        return entity.Comp.Budget;
    }

    public float? SetBudget(Entity<DynamicRuleComponent?> entity, float amount)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;

        entity.Comp.Budget = amount;
        return entity.Comp.Budget;
    }

    public IEnumerable<EntProtoId> DryRun(Entity<DynamicRuleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return new List<EntProtoId>();

        return GetRuleSpawns((entity.Owner, entity.Comp));
    }
    #endregion
}
