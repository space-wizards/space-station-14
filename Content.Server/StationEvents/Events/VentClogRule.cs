using System.Linq;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that spawn random reagent foam around a vent.
/// </summary>
[UsedImplicitly]
public sealed partial class VentClogRule : StationEventSystem<VentClogRuleComponent>
{
    [Dependency] private SmokeSystem _smoke = default!;

    protected override void Started(Entity<VentClogRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var ventClog = ent.Comp1;

        // TODO: "safe random" for chems. Right now this includes admin chemicals.
        var allReagents = ProtoMan.EnumeratePrototypes<ReagentPrototype>()
            .Where(x => !x.Abstract)
            .Select(x => new ProtoId<ReagentPrototype>(x.ID)).ToList();

        foreach (var ventPump in Station.GetEntitiesWithComponentOnStation<GasVentPumpComponent>(true))
        {
            var targetCoords = Transform(ventPump).Coordinates;
            var solution = new Solution();

            if (!RobustRandom.Prob(0.33f))
                continue;

            var pickAny = RobustRandom.Prob(0.05f);
            var reagent = RobustRandom.Pick(pickAny ? allReagents : ventClog.SafeishVentChemicals);

            var weak = ventClog.WeakReagents.Contains(reagent);
            var quantity = weak ? ventClog.WeakReagentQuantity : ventClog.ReagentQuantity;
            solution.AddReagent(reagent, quantity);

            var foamEnt = Spawn(ChemicalReactionSystem.FoamReaction, targetCoords);
            var spreadAmount = weak ? ventClog.WeakSpread : ventClog.Spread;
            _smoke.StartSmoke(foamEnt, solution, ventClog.Time, spreadAmount);
            Audio.PlayPvs(ventClog.Sound, targetCoords);
        }
    }
}
