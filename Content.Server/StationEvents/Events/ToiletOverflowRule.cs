using Content.Server.Station.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Fluids.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Toilet;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class ToiletOverflowRule : StationEventSystem<ToiletOverflowRuleComponent>
{
    [Dependency] private readonly PuddleSystem _puddle = default!;

    protected override void Started(EntityUid uid, ToiletOverflowRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        // TODO: "safe random" for chems. Right now this includes admin chemicals.
        var allReagents = PrototypeManager.EnumeratePrototypes<ReagentPrototype>()
            .Where(x => !x.Abstract)
            .Select(x => x.ID).ToList();

        // TODO: This is gross, but not much can be done until event refactor, which needs Dynamic.
        var mod = (float) Math.Sqrt(GetSeverityModifier());

        foreach (var (_, transform) in EntityManager.EntityQuery<ToiletComponent, TransformComponent>())
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station != chosenStation)
            {
                continue;
            }

            var solution = new Solution();

            if (!RobustRandom.Prob(Math.Min(0.33f * mod, 1.0f)))
                continue;

            var pickAny = RobustRandom.Prob(Math.Min(0.05f * mod, 1.0f));
            var reagent = RobustRandom.Pick(pickAny ? allReagents : component.SafeishToiletChemicals);

            var puddleEnt = Spawn("Puddle", transform.Coordinates);
            var puddle = EnsureComp<PuddleComponent>(puddleEnt);
            ///puddle.OverflowVolume = component.Spread;
            ///_puddle.Start(puddleEnt, puddle, solution, component.Time);
            Audio.PlayPvs(component.Sound, transform.Coordinates);
        }
    }
}
