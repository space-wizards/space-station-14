using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server._Impstation.CosmicCult;

public sealed class RogueAscendedObjectiveSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RogueInfectionConditionComponent, ObjectiveGetProgressEvent>(OnGetInfectionProgress);
    }

    private void OnGetInfectionProgress(EntityUid uid, RogueInfectionConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = InfectionProgress(comp, _number.GetTarget(uid));
    }

    private float InfectionProgress(RogueInfectionConditionComponent comp, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        return MathF.Min(comp.MindsCorrupted / (float) target, 1f);
    }

}
