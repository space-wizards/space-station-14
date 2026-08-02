// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Demons.Shadowling;
using Content.Shared.Objectives.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.DeadSpace.Demons.Shadowling;

public sealed class ShadowlingRecruitObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowlingRecruitObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<ShadowlingRecruitObjectiveComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnGetProgress(EntityUid uid, ShadowlingRecruitObjectiveComponent component, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity == null)
            return;

        var count = GetCount(args.Mind.OwnedEntity.Value, component);

        args.Progress = Math.Clamp((float)count / component.TargetCount, 0f, 1f);

        var description = Loc.GetString("shadowling-recruit-objective-desc",
            ("current", count),
            ("target", component.TargetCount));

        _metaData.SetEntityDescription(uid, description);
    }

    private void OnAfterAssign(EntityUid uid, ShadowlingRecruitObjectiveComponent component, ref ObjectiveAfterAssignEvent args)
    {
        var ruleQuery = EntityQueryEnumerator<ShadowlingRuleComponent>();
        while (ruleQuery.MoveNext(out var ruleComp))
        {
            component.TargetCount = (int)Math.Round(MathHelper.Lerp(component.MinTargetCount, component.MaxTargetCount, ruleComp.Scale));
        }

        _metaData.SetEntityName(uid, Loc.GetString("shadowling-recruit-objective-title"), args.Meta);

        int count = 0;
        if (args.Mind.OwnedEntity != null)
        {
            count = GetCount(args.Mind.OwnedEntity.Value, component);
        }

        _metaData.SetEntityDescription(uid, Loc.GetString("shadowling-recruit-objective-desc",
            ("current", count),
            ("target", component.TargetCount)), args.Meta);
    }

    private int GetCount(EntityUid master, ShadowlingRecruitObjectiveComponent component)
    {
        int count = 0;
        var query = EntityQueryEnumerator<ShadowlingSlaveComponent>();
        while (query.MoveNext(out var sUid, out var slave))
        {
            if (slave.Master == master && _mobState.IsAlive(sUid))
                count++;
        }
        return count;
    }
}