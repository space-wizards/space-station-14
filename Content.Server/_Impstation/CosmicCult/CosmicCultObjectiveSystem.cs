using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class CosmicCultObjectiveSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicEntropyConditionComponent, ObjectiveGetProgressEvent>(OnGetEntropyProgress);
        SubscribeLocalEvent<CosmicTierConditionComponent, ObjectiveGetProgressEvent>(OnGetTierProgress);
        SubscribeLocalEvent<CosmicVictoryConditionComponent, ObjectiveGetProgressEvent>(OnGetVictoryProgress);
    }

    private void OnGetEntropyProgress(Entity<CosmicEntropyConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = Progress(ent.Comp.Siphoned, _number.GetTarget(ent.Owner));
    }

    private void OnGetTierProgress(Entity<CosmicTierConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = Progress(ent.Comp.Tier, _number.GetTarget(ent.Owner));
    }

    private void OnGetVictoryProgress(Entity<CosmicVictoryConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = Progress(ent.Comp.Victory, _number.GetTarget(ent.Owner));
    }

    private static float Progress(int recruited, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        return MathF.Min(recruited / (float)target, 1f);
    }

}
