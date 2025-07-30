using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Medical.Healing;

namespace Content.Shared.Medical;

public sealed class SharedHealingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealingComponent, SolutionTransferAttemptEvent>(OnHealingSolutionTransferAttempt);
    }

    private void OnHealingSolutionTransferAttempt(Entity<HealingComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        var (uid, component) = ent;
        if (component.SolutionDrain)
        {
            var solution = args.SolutionEntity.Comp.Solution;
            if (solution.Contents.Any(sol => !component.ReagentsToDrain.Any(req => req.Reagent == sol.Reagent)))
                args.Cancel("This solution contains unsuitable reagents!");
        }
    }
}
