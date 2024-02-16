using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Station.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class VentClogRule : StationEventSystem<VentClogRuleComponent>
{
    [Dependency] private readonly SmokeSystem _smoke = default!;

    protected override void Started(EntityUid uid, VentClogRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
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

        foreach (var (_, transform) in EntityManager.EntityQuery<GasVentPumpComponent, TransformComponent>())
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station != chosenStation)
            {
                continue;
            }

            var solution = new Solution();

            if (!RobustRandom.Prob(Math.Min(0.33f * mod, 1.0f)))
                continue;

            var pickAny = RobustRandom.Prob(Math.Min(0.05f * mod, 1.0f));
            var reagent = RobustRandom.Pick(pickAny ? allReagents : component.SafeishVentChemicals);

            var weak = component.WeakReagents.Contains(reagent);
            var quantity = (weak ? component.WeakReagentQuantity : component.ReagentQuantity) * mod;
            solution.AddReagent(reagent, quantity);

            var foamEnt = Spawn("Foam", transform.Coordinates);
            var spreadAmount = weak ? component.WeakSpread : component.Spread;
            _smoke.StartSmoke(foamEnt, solution, component.Time, spreadAmount);
            Audio.PlayPvs(component.Sound, transform.Coordinates);
        }
    }
}
