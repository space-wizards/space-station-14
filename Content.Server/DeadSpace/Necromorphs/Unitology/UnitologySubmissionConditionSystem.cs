// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Objectives.Components;
using Content.Server.DeadSpace.Necromorphs.Unitology.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.Humanoid;

namespace Content.Server.DeadSpace.Necromorphs.Unitology;

public sealed class UnitologySubmissionConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UnitologySubmissionConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, UnitologySubmissionConditionComponent component, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = SubordinationOfEnslavedProgress(component, _number.GetTarget(uid));
    }

    private float SubordinationOfEnslavedProgress(UnitologySubmissionConditionComponent component, int target)
    {
        if (target == 0)
            return 1f;

        float count = 0;

        var query = AllEntityQuery<UnitologyEnslavedComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            if (HasComp<HumanoidAppearanceComponent>(ent))
                count++;
        }

        component.Progress = MathF.Min((float)count / (float)target, 1f);

        return component.Progress;
    }
}
