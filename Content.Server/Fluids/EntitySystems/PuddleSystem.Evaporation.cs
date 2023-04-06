using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    private static readonly TimeSpan EvaporationCooldown = TimeSpan.FromSeconds(1);
    private static readonly FixedPoint2 EvaporationAmount = FixedPoint2.New(2);

    private const string EvaporationReagent = "Water";

    private void OnEvaporationMapInit(EntityUid uid, EvaporationComponent component, MapInitEvent args)
    {
        component.NextTick = _timing.CurTime + EvaporationCooldown;
    }

    private void UpdateEvaporation(EntityUid uid, Solution solution)
    {
        if (TryComp<EvaporationComponent>(uid, out var evaporation))
        {
            // If we won't evaporate stop sparkling.
            if (CanSparkle(solution))
            {
                SetEvaporationSparkle(uid, true);
            }
            else
            {
                SetEvaporationSparkle(uid, false);
            }

            return;
        }

        if (solution.ContainsReagent(EvaporationReagent))
        {
            evaporation = AddComp<EvaporationComponent>(uid);
            evaporation.NextTick = _timing.CurTime + EvaporationCooldown;
            return;
        }

        RemComp<EvaporationComponent>(uid);
    }

    private void TickEvaporation()
    {
        var query = EntityQueryEnumerator<EvaporationComponent, PuddleComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var evaporation, out var puddle))
        {
            if (evaporation.NextTick > curTime)
                continue;

            evaporation.NextTick += EvaporationCooldown;

            if (!_solutionContainerSystem.TryGetSolution(uid, puddle.SolutionName, out var puddleSolution))
                continue;

            puddleSolution.RemoveReagent(EvaporationReagent, EvaporationAmount);

            // Despawn if we're done
            if (puddleSolution.Volume == FixedPoint2.Zero)
            {
                // TODO: Standalone sparkle entity?
                QueueDel(uid);
                continue;
            }

            // If we only contain the evaporation reagent then *sparkle*
            if (CanSparkle(puddleSolution))
            {
                SetEvaporationSparkle(uid, true);
            }
            else
            {
                SetEvaporationSparkle(uid, false);
            }
        }
    }

    private bool CanSparkle(Solution solution)
    {
        return solution.Contents.Count == 1 && solution.ContainsReagent(EvaporationReagent);
    }

    private void SetEvaporationSparkle(EntityUid uid, bool enabled)
    {
        _appearance.SetData(uid, PuddleVisuals.Evaporation, enabled);
    }
}
