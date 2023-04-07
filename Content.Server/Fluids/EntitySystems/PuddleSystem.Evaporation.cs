using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    private static readonly TimeSpan EvaporationCooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Assuming puddle is 20 units then ~1 minute for it to evaporate.
    /// </summary>
    private static readonly FixedPoint2 EvaporationAmount = FixedPoint2.New(0.3);

    public const string EvaporationReagent = "Water";

    private void OnEvaporationMapInit(EntityUid uid, EvaporationComponent component, MapInitEvent args)
    {
        component.NextTick = _timing.CurTime + EvaporationCooldown;
    }

    private void UpdateEvaporation(EntityUid uid, Solution solution)
    {
        if (HasComp<EvaporationComponent>(uid))
        {
            return;
        }

        if (solution.ContainsReagent(EvaporationReagent))
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
        var xformQuery = GetEntityQuery<TransformComponent>();
        var curTime = _timing.CurTime;
        var reagentTick = EvaporationAmount * EvaporationCooldown.TotalSeconds;

        while (query.MoveNext(out var uid, out var evaporation, out var puddle))
        {
            if (evaporation.NextTick > curTime)
                continue;

            evaporation.NextTick += EvaporationCooldown;

            if (!_solutionContainerSystem.TryGetSolution(uid, puddle.SolutionName, out var puddleSolution))
                continue;

            puddleSolution.RemoveReagent(EvaporationReagent, reagentTick);

            // Despawn if we're done
            if (puddleSolution.Volume == FixedPoint2.Zero)
            {
                // Spawn a *sparkle*
                Spawn("PuddleSparkle", xformQuery.GetComponent(uid).Coordinates);
                QueueDel(uid);
            }
        }
    }

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.Contents.Count == 1 && solution.ContainsReagent(EvaporationReagent);
    }
}
