using Content.Server.Objectives.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class SupercriticalAnomaliesConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _numberObjective = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyShutdownEvent>(OnAnomalySupercrit);
        SubscribeLocalEvent<SupercriticalAnomaliesConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAnomalySupercrit(ref AnomalyShutdownEvent args)
    {
        if (!args.Supercritical)
            return;

        var query = EntityQueryEnumerator<SupercriticalAnomaliesConditionComponent>();
        while (query.MoveNext(out var comp))
        {
            comp.SupercriticalAnomalies += 1;
        }
    }

    private void OnGetProgress(Entity<SupercriticalAnomaliesConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var target = _numberObjective.GetTarget(ent);
        if (target == 0)
        {
            args.Progress = 0f;
            return;
        }
        args.Progress = MathF.Min((float)ent.Comp.SupercriticalAnomalies / target, 1f);
    }
}
