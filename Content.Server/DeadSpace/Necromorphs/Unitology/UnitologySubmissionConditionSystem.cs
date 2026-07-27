// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Objectives.Components;
using Content.Server.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.Humanoid;
using Robust.Server.Player;

namespace Content.Server.DeadSpace.Necromorphs.Unitology;

public sealed class UnitologySubmissionConditionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UnitologySubmissionConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<UnitologySubmissionConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAfterAssign(Entity<UnitologySubmissionConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        ent.Comp.Target = GetTarget();
        _metaData.SetEntityName(ent.Owner,
            Loc.GetString("objective-condition-unitology-slaves-title", ("count", ent.Comp.Target)),
            args.Meta);
    }

    private void OnGetProgress(EntityUid uid, UnitologySubmissionConditionComponent component, ref ObjectiveGetProgressEvent args)
    {
        if (component.Target <= 0)
            component.Target = GetTarget();

        component.Progress = CalculateProgress(component.Target);
        args.Progress = component.Progress;
    }

    public int GetTarget()
    {
        return 3 + Math.Max(0, (_players.PlayerCount - 65) / 35);
    }

    public bool TryGetAssignedTarget(EntityUid objective, out int target)
    {
        target = 0;
        if (!TryComp<UnitologySubmissionConditionComponent>(objective, out var component) || component.Target <= 0)
            return false;

        target = component.Target;
        return true;
    }

    public float CalculateProgress(int target)
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

        return MathF.Min((float)count / target, 1f);
    }
}
