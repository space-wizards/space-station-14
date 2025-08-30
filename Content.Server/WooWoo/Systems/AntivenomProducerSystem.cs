using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.WooWoo.Components.Antivenom;
using Content.Shared.WooWoo.Systems.Antivenom;
using Robust.Shared.Prototypes;

namespace Content.Server.WooWoo.Systems.Antivenom;

/// <summary>
/// Handles creating antivenom when an entity has developed an immunoresponse to a reagent.
/// </summary>
public sealed class AntivenomProducerSystem : SharedAntivenomProducerSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>
    /// Actually add reagents to a solution on the server. Called by the shared system each production tick.
    /// </summary>
    public override void CreateAntivenom(
        Entity<SolutionComponent> soln,
        AntivenomProducerComponent comp
        )
    {
        if (comp.ImmunoCompromised || comp.UnlockedImmunities.Count == 0)
            return;

        // handle multiple venoms with same antivenom
        var maxdelta = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>();

        foreach (var (venom, stage) in comp.UnlockedImmunities)
        {
            if (!comp.ConfigByVenom.TryGetValue(venom, out var cfg))
                continue;

            var amount = cfg.AVPerStage * stage;
            if (amount <= FixedPoint2.Zero)
                continue;

            // do up to the highest MaxBuffer adding all antivenom configs of same reagent
            var delta = maxdelta.TryGetValue(cfg.Antivenom, out var cum)
                ? (cum > cfg.MaxBuffer ? cum : FixedPoint2.Min(cfg.MaxBuffer, amount + cum))
                : FixedPoint2.Min(cfg.MaxBuffer, amount);

            // account for current reagents in solution
            var cur = soln.Comp.Solution.GetTotalPrototypeQuantity(cfg.Antivenom);
            if (delta <= cur)
                continue;

            maxdelta[cfg.Antivenom] = delta - cur;
        }

        if (maxdelta.Count == 0)
            return;

        // Add each antivenom reagent.
        foreach (var (antivenom, amount) in maxdelta)
        {
            if (amount <= FixedPoint2.Zero)
                continue;

            _solutions.TryAddReagent(soln, antivenom, amount, out var actuallyAdded);
            if (actuallyAdded != amount)
                Log.Warning($"{amount.Float()} of {antivenom.ToString()} was added to {ToPrettyString(soln)} as antivenom, but only {actuallyAdded.Float()} fit!");
            // ^ if this is firing it means an ent is filling its chemstream. Probably not because of us, but this lets us know to check.
        }
    }
}
