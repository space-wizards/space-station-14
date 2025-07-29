using System;
using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Shared._Starlight.Abstract.Conditions;
using Content.Shared._Starlight.Railroading;
using Content.Shared.Alert;
using Content.Shared.GameTicking.Components;
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

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Added(EntityUid uid, RailroadRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
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
                var cardsToIssue = Math.Min(CardPerUser - subject.Comp.IssuedCards.Count, ruleEnt.Comp.Pool.Count);
                for (var i = 0; i < cardsToIssue; i++)
                {
                    var card = PopRandomFromProcessingPool(ruleEnt, subject);
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
            var ruleOwner = EnsureComp<RuleOwnerComponent>(eid);
            ruleOwner.RuleOwner = ruleEnt.Owner;
            ruleEnt.Comp.Pool.Add((eid, cardComp, ruleOwner));
        }
    }

    private Entity<RailroadCardComponent, RuleOwnerComponent>? PopRandomFromProcessingPool(Entity<RailroadRuleComponent> ruleEnt, EntityUid subject)
    {
        var startIndex = _random.Next(ruleEnt.Comp.Pool.Count);
        for (var i = startIndex; i < ProcessingMaxTry + startIndex; i++)
        {
            var index = i % ruleEnt.Comp.Pool.Count;
            var card = ruleEnt.Comp.Pool[index];
            if (Deleted(card))
            {
                ruleEnt.Comp.Pool.RemoveSwapBack(index);
                continue;
            }
            if (TryComp<ConditionsComponent>(card, out var conditions)
                && !conditions.Conditions.All(x => x.Handle(subject, card)))
                continue;
            ruleEnt.Comp.Pool.RemoveSwapBack(index);
            return card;
        }
        return null;
    }
    #endregion
}