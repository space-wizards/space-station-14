using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Shared._Starlight.Abstract.Conditions;
using Content.Shared._Starlight.Railroading;
using Content.Shared.Alert;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.Starlight;
using Content.Shared.Store;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using static Content.Shared._Starlight.Railroading.RailroadRuleComponent;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadRuleSystem : GameRuleSystem<RailroadRuleComponent>
{
    // it’s something that should be synchronized across all rules.
    private const byte CardPerUser = 3;
    private const byte SpawnPerTick = 10;
    private const byte ProcessingMaxTry = 10;

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _comp = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly RailroadingSystem _railroading = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Added(EntityUid uid, RailroadRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (TryGetActiveRule(out var rule))
        {
            foreach (var card in rule.Cards)
                comp.DynamicCards.Enqueue(card);
            _gameTicker.EndGameRule(uid);
            return;
        }
        base.Added(uid, comp, gameRule, args);
        comp.Timer = comp.PreSpawnDelay;
    }


    protected override void ActiveTick(EntityUid uid, RailroadRuleComponent comp, GameRuleComponent gameRule, float frameTime)
    => _ = comp.Stage switch
    {
        RailroadStage.PreSpawnDelay => TickPreSpawnDelay((uid, comp), frameTime),
        RailroadStage.StaticSpawn => TickStaticSpawn((uid, comp)),
        RailroadStage.DynamicSpawn => TickDynamicSpawn((uid, comp)),
        RailroadStage.CardShuffle => TickCardShuffle((uid, comp)),
        RailroadStage.PreCardIssuance => TickPreCardIssuance((uid, comp)),
        RailroadStage.CardIssuance => TickCardIssuance((uid, comp)),
        RailroadStage.CycleDelay => TickCycleDelay((uid, comp), frameTime),
        _ => false
    };

    #region ——— Stage Tick Methods ———

    private static bool TickPreSpawnDelay(Entity<RailroadRuleComponent> ruleEnt, float dt)
    {
        ruleEnt.Comp.Timer -= dt;
        if (ruleEnt.Comp.Timer > 0f)
            return true;

        ruleEnt.Comp.Stage = RailroadStage.StaticSpawn;
        return true;
    }

    private bool TickStaticSpawn(Entity<RailroadRuleComponent> ruleEnt)
    {
        for (var i = 0; i < SpawnPerTick && ruleEnt.Comp.SpawnIndex < ruleEnt.Comp.Cards.Count; i++)
        {
            RegisterCardInstance(ruleEnt.Comp.Cards[ruleEnt.Comp.SpawnIndex], ruleEnt);
            ruleEnt.Comp.SpawnIndex++;
        }

        if (ruleEnt.Comp.SpawnIndex >= ruleEnt.Comp.Cards.Count)
            ruleEnt.Comp.Stage = RailroadStage.DynamicSpawn;
        return true;
    }

    private bool TickDynamicSpawn(Entity<RailroadRuleComponent> ruleEnt)
    {
        for (var i = 0; i < SpawnPerTick && ruleEnt.Comp.DynamicCards.TryDequeue(out var card); i++)
        {
            RegisterCardInstance(card, ruleEnt);
            ruleEnt.Comp.SpawnIndex++;
        }

        if (ruleEnt.Comp.DynamicCards.Count == 0)
            ruleEnt.Comp.Stage = RailroadStage.CardShuffle;
        return true;
    }

    // The shuffle is done because after a card’s condition isn’t met, we take the next card in order
    // and this prevents us from running into 10 cards in a row with the same conditions.
    // At the same time, for the job-specific pool this isn’t needed, since there are only a few cards there.
    private bool TickCardShuffle(Entity<RailroadRuleComponent> ruleEnt)
    {
        _random.Shuffle(ruleEnt.Comp.Pool);
        ruleEnt.Comp.Stage = RailroadStage.PreCardIssuance;

        return true;
    }

    private bool TickPreCardIssuance(Entity<RailroadRuleComponent> ruleEnt)
    {
        var query = EntityQueryEnumerator<RailroadableComponent>();

        while (query.MoveNext(out var uid, out var railroadableComp))
            if (_players.TryGetSessionByEntity(uid, out _))
                ruleEnt.Comp.IssuanceQueue.Enqueue((uid, railroadableComp));

        ruleEnt.Comp.Stage = RailroadStage.CardIssuance;

        return true;
    }

    private bool TickCardIssuance(Entity<RailroadRuleComponent> ruleEnt)
    {
        if (ruleEnt.Comp.Pool.Count > 0 && ruleEnt.Comp.IssuanceQueue.TryDequeue(out var subject))
        {
            if (!Deleted(subject.Owner)
                && !subject.Comp.Restricted
                && (subject.Comp.ActiveCard == null || Deleted(subject.Comp.ActiveCard)))
            {
                subject.Comp.IssuedCards ??= new List<Entity<RailroadCardComponent, RuleOwnerComponent>>(CardPerUser);
                var cardsToIssue = CardPerUser - subject.Comp.IssuedCards.Count;
                for (var i = 0; i < cardsToIssue; i++)
                {
                    if(subject.Comp.IssuedCards.Count == 1
                        && TryGetJobSpecificPool(ruleEnt, subject, out var jobPool))
                    {
                        var jobCard = PopRandomFromPool(jobPool, subject);
                        if (jobCard is not null)
                        {
                            subject.Comp.IssuedCards.Add(jobCard.Value);
                            continue;
                        }
                    }

                    var card = PopRandomFromPool(ruleEnt.Comp.Pool, subject);
                    if (card == null)
                        break;

                    subject.Comp.IssuedCards.Add(card.Value);
                }

                if (subject.Comp.IssuedCards.Count > 0)
                    _railroading.ShowAlert(subject.Owner);
            }
        }
        else
        {
            ruleEnt.Comp.Timer = ruleEnt.Comp.Delay;
            ruleEnt.Comp.Stage = RailroadStage.CycleDelay;
        }
        return true;
    }

    private static bool TickCycleDelay(Entity<RailroadRuleComponent> ruleEnt, float dt)
    {
        ruleEnt.Comp.Timer -= dt;
        if (ruleEnt.Comp.Timer > 0f)
            return true;

        ruleEnt.Comp.Stage = RailroadStage.DynamicSpawn;
        return true;
    }

    #endregion

    public void AddCardToPool(Entity<RailroadRuleComponent> ruleEnt, Entity<RailroadCardComponent> card)
    {
        var ruleOwner = EnsureComp<RuleOwnerComponent>(card.Owner);
        ruleOwner.RuleOwner = ruleEnt.Owner;

        if (TryComp<RailroadSpawnFlowComponent>(card.Owner, out var flow) && flow.JobPrototype is { } job)
        {
            if (ruleEnt.Comp.PoolByJob.TryGetValue(job, out var list))
                list.Add((card.Owner, card.Comp, ruleOwner));
            else
                ruleEnt.Comp.PoolByJob.Add(job, [(card.Owner, card.Comp, ruleOwner)]);
        }
        else
        {
            ruleEnt.Comp.Pool.Add((card.Owner, card.Comp, ruleOwner));
        }
    }

    #region ——— Helpers ———

    private void RegisterCardInstance(EntProtoId<RailroadCardComponent> proto, Entity<RailroadRuleComponent> ruleEnt)
    {
        if (!_proto.TryIndex(proto, out var cardProto))
            return;

        // You’re probably going to ask why the entity itself holds information about how to spawn it.
        // Yes.
        if (cardProto.TryGetComponent<RailroadSpawnFlowComponent>(out var flow, _comp))
        {
            if (flow.Probability < 1.0f && !_random.Prob(flow.Probability))
                return;

            for (var i = 0; i < flow.Count.Next(_random); i++)
                Register(proto, ruleEnt);
        }
        else
            Register(proto, ruleEnt);

        void Register(EntProtoId<RailroadCardComponent> proto, Entity<RailroadRuleComponent> ruleEnt)
        {
            var eid = Spawn(proto, MapCoordinates.Nullspace);
            var cardComp = EnsureComp<RailroadCardComponent>(eid);
            AddCardToPool(ruleEnt, (eid, cardComp));
        }
    }

    private Entity<RailroadCardComponent, RuleOwnerComponent>? PopRandomFromPool(List<Entity<RailroadCardComponent, RuleOwnerComponent>> pool, EntityUid subject)
    {
        var startIndex = _random.Next(pool.Count);
        for (var i = startIndex; i < ProcessingMaxTry + startIndex && pool.Count > 0; i++)
        {
            var index = i % pool.Count;
            var card = pool[index];
            if (Deleted(card))
            {
                pool.RemoveSwapBack(index);
                continue;
            }
            if (TryComp<ConditionsComponent>(card, out var conditions)
                && !conditions.Conditions.All(x => x.Handle(subject, card)))
                continue;
            pool.RemoveSwapBack(index);
            return card;
        }
        return null;
    }

    private bool TryGetActiveRule([NotNullWhen(true)] out RailroadRuleComponent? rule)
    {
        rule = null;
        var query = EntityQueryEnumerator<RailroadRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2))
        {
            if (!GameTicker.IsGameRuleActive(uid, comp2))
                continue;

            rule = comp1;
            return true;
        }
        return false;
    }

    private bool TryGetJobSpecificPool
    (
        Entity<RailroadRuleComponent> ruleEnt,
        Entity<RailroadableComponent> subject,
        [NotNullWhen(true)] out List<Entity<RailroadCardComponent, RuleOwnerComponent>>? pool
    )
    {
        if (_mind.TryGetMind(subject.Owner, out var mindUid, out var mind)
            && _job.MindTryGetJobId(mindUid, out var job)
            && job != null
            && ruleEnt.Comp.PoolByJob.TryGetValue(job.Value, out var jobPool)
            && jobPool.Count > 0)
        {
            pool = jobPool;
            return true;
        }
        pool = null;
        return false;
    }
    #endregion
}