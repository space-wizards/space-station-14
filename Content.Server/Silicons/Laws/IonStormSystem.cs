using Content.Server.StationEvents.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Silicons.Laws;

public sealed class IonStormSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    /// <summary>
    /// Randomly alters the laws of an individual silicon.
    /// </summary>
    public void IonStormTarget(Entity<SiliconLawBoundComponent, IonStormTargetComponent> ent, bool adminlog = true)
    {
        var lawBound = ent.Comp1;
        var target = ent.Comp2;
        if (!_robustRandom.Prob(target.Chance))
            return;

        var laws = _siliconLaw.GetLaws(ent, lawBound);
        if (laws.Laws.Count == 0)
            return;

        // try to swap it out with a random lawset
        if (_robustRandom.Prob(target.RandomLawsetChance))
        {
            var lawsets = _proto.Index<WeightedRandomPrototype>(target.RandomLawsets);
            var lawset = lawsets.Pick(_robustRandom);
            laws = _siliconLaw.GetLawset(lawset);
        }
        // clone it so not modifying stations lawset
        laws = laws.Clone();

        // shuffle them all
        if (_robustRandom.Prob(target.ShuffleChance))
        {
            // hopefully work with existing glitched laws if there are multiple ion storms
            var baseOrder = FixedPoint2.New(1);
            foreach (var law in laws.Laws)
            {
                if (law.Order < baseOrder)
                    baseOrder = law.Order;
            }

            _robustRandom.Shuffle(laws.Laws);

            // change order based on shuffled position
            for (int i = 0; i < laws.Laws.Count; i++)
            {
                laws.Laws[i].Order = baseOrder + i;
            }
        }

        // see if we can remove a random law
        if (laws.Laws.Count > 0 && _robustRandom.Prob(target.RemoveChance))
        {
            var i = _robustRandom.Next(laws.Laws.Count);
            laws.Laws.RemoveAt(i);
        }

        // generate a new law...
        var newLaw = GenerateLaw();

        if (string.IsNullOrEmpty(newLaw))
            return;

        // see if the law we add will replace a random existing law or be a new glitched order one
        if (laws.Laws.Count > 0 && _robustRandom.Prob(target.ReplaceChance))
        {
            var i = _robustRandom.Next(laws.Laws.Count);
            laws.Laws[i] = new SiliconLaw()
            {
                LawString = newLaw,
                Order = laws.Laws[i].Order
            };
        }
        else
        {
            laws.Laws.Insert(0, new SiliconLaw
            {
                LawString = newLaw,
                Order = -1,
                LawIdentifierOverride = Loc.GetString("ion-storm-law-scrambled-number", ("length", _robustRandom.Next(5, 10)))
            });
        }

        // sets all unobfuscated laws' indentifier in order from highest to lowest priority
        // This could technically override the Obfuscation from the code above, but it seems unlikely enough to basically never happen
        int orderDeduction = -1;

        for (int i = 0; i < laws.Laws.Count; i++)
        {
            var notNullIdentifier = laws.Laws[i].LawIdentifierOverride ?? (i - orderDeduction).ToString();

            if (notNullIdentifier.Any(char.IsSymbol))
            {
                orderDeduction += 1;
            }
            else
            {
                laws.Laws[i].LawIdentifierOverride = (i - orderDeduction).ToString();
            }
        }

        // adminlog is used to prevent adminlog spam.
        if (adminlog)
            _adminLogger.Add(LogType.Mind, LogImpact.High, $"{ToPrettyString(ent):silicon} had its laws changed by an ion storm to {laws.LoggingString()}");

        // laws unique to this silicon, dont use station laws anymore
        EnsureComp<SiliconLawProviderComponent>(ent);
        var ev = new IonStormLawsEvent(laws);
        RaiseLocalEvent(ent, ref ev);
    }

    private string GenerateLaw()
    {
        var ionLaws = _proto.EnumeratePrototypes<IonLawPrototype>().ToList();
        if (ionLaws.Count == 0)
            return string.Empty;

        var totalWeight = ionLaws.Sum(x => x.Weight);
        if (totalWeight <= 0)
            return string.Empty;

        while (ionLaws.Count > 0)
        {
            var law = PickLaw(ionLaws, totalWeight);
            ionLaws.Remove(law);
            totalWeight -= law.Weight;

            var args = new List<(string, object)>();
            var allTargetsMatched = true;
            foreach (var (key, selectors) in law.Targets)
            {
                var value = SelectTarget(selectors);
                if (value == null)
                {
                    allTargetsMatched = false;
                    break;
                }

                args.Add((key, value));
            }

            if (allTargetsMatched)
                return Loc.GetString(law.LawString, args.ToArray());
        }

        return string.Empty;
    }

    private IonLawPrototype PickLaw(List<IonLawPrototype> laws, float totalWeight)
    {
        var r = _robustRandom.NextFloat() * totalWeight;
        foreach (var law in laws)
        {
            r -= law.Weight;
            if (r <= 0)
                return law;
        }
        return laws.Last();
    }

    private object? SelectTarget(List<IonLawSelector> selectors)
    {
        var shuffledSelectors = selectors.ToList();
        _robustRandom.Shuffle(shuffledSelectors);

        foreach (var selector in shuffledSelectors)
        {
            var value = selector.Select(_robustRandom, _proto, EntityManager, null);
            if (value != null)
                return value;
        }

        return null;
    }
}
