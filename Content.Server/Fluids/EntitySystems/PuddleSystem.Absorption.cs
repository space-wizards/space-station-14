using Content.Server.Body.Components;
using Content.Shared.Fluids.Components;
using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    private static readonly TimeSpan AbsorptionCooldown = TimeSpan.FromSeconds(1);

    private void OnAbsorptionMapInit(Entity<PuddleAbsorptionComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.NextTick = _timing.CurTime + AbsorptionCooldown;
    }
    private void AbsorbPuddleByEntities()
    {
        var query = EntityQueryEnumerator<StepTriggerComponent, PuddleComponent>();

        while (query.MoveNext(out var puddleUid, out var stepTriggerComp, out var puddleComp))
        {
            var stepTriggerEnumerator = stepTriggerComp.CurrentlySteppedOn.GetEnumerator();

            while (stepTriggerEnumerator.MoveNext())
            {
                var entityUid = stepTriggerEnumerator.Current;

                if (!TryComp<PuddleAbsorptionComponent>(entityUid, out var puddleAbsorptionComp))
                    return;

                var curTime = _timing.CurTime;
                if (puddleAbsorptionComp.NextTick > curTime)
                    return;

                puddleAbsorptionComp.NextTick += AbsorptionCooldown;

                if (!TryComp<BloodstreamComponent>(entityUid, out var bloodstreamComp))
                    return;

                if (!_solutionContainerSystem.ResolveSolution(puddleUid, puddleComp.SolutionName, ref puddleComp.Solution,
                out var solution))
                    return;

                var removedSolution = _solutionContainerSystem.SplitSolution(puddleComp.Solution.Value, puddleAbsorptionComp.AmountPerTick);

                _blood.TryAddToChemicals(entityUid, removedSolution, bloodstreamComp);
                _reactive.DoEntityReaction(entityUid, removedSolution, ReactionMethod.Injection);
            }
        }
    }
}
