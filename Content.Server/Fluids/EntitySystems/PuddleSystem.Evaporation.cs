using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    private static readonly TimeSpan EvaporationCooldown = TimeSpan.FromSeconds(1);

    private void OnEvaporationMapInit(Entity<EvaporationComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.NextTick = _timing.CurTime + EvaporationCooldown;
    }

    private void UpdateEvaporation(EntityUid uid, Solution solution)
    {
        if (HasComp<EvaporationComponent>(uid))
        {
            return;
        }

        if (solution.GetTotalPrototypeQuantity(EvaporationReagents) > FixedPoint2.Zero)
        {
            var evaporation = AddComp<EvaporationComponent>(uid);
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

            var tileMix = _atmosphereSystem.GetContainingMixture(uid, true);
            var hasEnoughVapor = tileMix?.GetMoles(Gas.WaterVapor) >= 1.0f;

            if (hasEnoughVapor)
                EnsureComp<PreventEvaporationComponent>(uid).Active = true;
            else
                RemComp<PreventEvaporationComponent>(uid);

            if (!hasEnoughVapor)
            {
                evaporation.NextTick = _timing.CurTime + EvaporationCooldown;
                Dirty(uid, evaporation);

                if (_solutionContainerSystem.ResolveSolution(uid, puddle.SolutionName, 
                    ref puddle.Solution, out var puddleSolution))
                {
                    var reagentTick = evaporation.EvaporationAmount * EvaporationCooldown.TotalSeconds;
                    puddleSolution.SplitSolutionWithOnly(reagentTick, EvaporationReagents);

                    if (puddleSolution.Volume == FixedPoint2.Zero)
                    {
                        // Spawn a *sparkle*
                        Spawn("PuddleSparkle", Transform(uid).Coordinates);
                        QueueDel(uid);
                    }

                    _solutionContainerSystem.UpdateChemicals(puddle.Solution.Value);
                }
            }
        }
    }
}
