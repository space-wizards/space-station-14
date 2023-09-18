using Content.Server.GameTicking.Rules.Components;
using Content.Server.Silicons.Laws;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;

    protected override void Started(EntityUid uid, IonStormRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var query = EntityQueryEnumerator<SiliconLawBoundComponent, TransformComponent, IonStormTargetComponent>();
        while (query.MoveNext(out var ent, out var lawBound, out var xform, out var target))
        {
            // only affect law holders on the station
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;

            if (!RobustRandom.Prob(target.Chance))
                continue;

            var laws = _siliconLaw.GetLaws(ent, lawBound);
            if (laws.Laws.Count == 0)
                continue;

            // try to swap it out with a random lawset
            if (RobustRandom.Prob(target.RandomLawsetChance))
            {
                var lawsets = PrototypeManager.Index<WeightedRandomPrototype>(target.RandomLawsets);
                var lawset = lawsets.Pick(RobustRandom);
                laws = _siliconLaw.GetLawset(lawset);
            }
            else
            {
                // clone it so not modifying stations lawset
                laws = laws.Clone();
            }

            // shuffle them all
            if (RobustRandom.Prob(target.ShuffleChance))
            {
                // hopefully work with existing glitched laws if there are multiple ion storms
                FixedPoint2 baseOrder = FixedPoint2.New(1);
                foreach (var law in laws.Laws)
                {
                    if (law.Order < baseOrder)
                        baseOrder = law.Order;
                }

                RobustRandom.Shuffle(laws.Laws);

                // change order based on shuffled position
                for (int i = 0; i < laws.Laws.Count; i++)
                {
                    laws.Laws[i].Order = baseOrder + i;
                }
            }

            // see if we can remove a random law
            if (laws.Laws.Count > 0 && RobustRandom.Prob(target.RemoveChance))
            {
                var i = RobustRandom.Next(laws.Laws.Count);
                laws.Laws.RemoveAt(i);
            }

            // generate a new law...
            var newLaw = GenerateLaw();

            // see if the law we add will replace a random existing law or be a new glitched order one
            if (laws.Laws.Count > 0 && RobustRandom.Prob(target.ReplaceChance))
            {
                var i = RobustRandom.Next(laws.Laws.Count);
                laws.Laws[i] = new SiliconLaw()
                {
                    LawString = newLaw,
                    Order = laws.Laws[i].Order
                };
            }
            else
            {
                laws.Laws.Insert(0, new SiliconLaw()
                {
                    LawString = newLaw,
                    Order = -1,
                    LawIdentifierOverride = "#"
                });
            }

            _adminLogger.Add(LogType.Mind, LogImpact.High, $"{ToPrettyString(ent):silicon} had its laws changed by an ion storm to {laws.LoggingString()}");

            // laws unique to this silicon, dont use station laws anymore
            EnsureComp<SiliconLawProviderComponent>(ent);
            var ev = new IonStormLawsEvent(laws);
            RaiseLocalEvent(ent, ref ev);
        }
    }

    private string GenerateLaw()
    {
        // TODO
        return "Oxygen is harmful to humans.";
    }
}
