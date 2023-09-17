using Content.Server.GameTicking.Rules.Components;
using Content.Server.Silicons.Laws;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
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

            var laws = _siliconLaw.GetLaws(ent, lawBound);
            if (laws.Laws.Count == 0)
                continue;

            // first try to swap it out with a random lawset
            if (RobustRandom.Prob(target.RandomLawsetChance))
            {
                var lawsets = PrototypeManager.Index<WeightedRandomPrototype>(target.RandomLawsets);
                var lawset = lawsets.Pick(RobustRandom);
                laws = _siliconLaw.GetLawset(lawset);
            }

            // second see if we can remove a random law
            if (laws.Laws.Count > 0 && RobustRandom.Prob(target.RemoveChance))
            {
                var i = RobustRandom.Next(laws.Laws.Count);
                laws.Laws.RemoveAt(i);
            }

            // third generate a new law...
            var newLaw = GenerateLaw();

            // fourth see if the law we add will replace a random existing law or be a new glitched order one
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

            // fifth shuffle them all
            if (RobustRandom.Prob(target.ShuffleChance))
                RobustRandom.Shuffle(laws.Laws);

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
