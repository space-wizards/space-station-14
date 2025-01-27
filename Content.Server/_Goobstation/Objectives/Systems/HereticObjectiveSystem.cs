using Content.Server._Goobstation.Objectives.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class HereticObjectiveSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticKnowledgeConditionComponent, ObjectiveGetProgressEvent>(OnGetKnowledgeProgress);
        SubscribeLocalEvent<HereticSacrificeConditionComponent, ObjectiveGetProgressEvent>(OnGetSacrificeProgress);
    }

    private void OnGetKnowledgeProgress(Entity<HereticKnowledgeConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(ent);
        if (target != 0)
            args.Progress = MathF.Min(ent.Comp.Researched / target, 1f);
        else args.Progress = 1f;
    }
    private void OnGetSacrificeProgress(Entity<HereticSacrificeConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(ent);
        if (target != 0)
            args.Progress = MathF.Min(ent.Comp.Sacrificed / target, 1f);
        else args.Progress = 1f;
    }
}
