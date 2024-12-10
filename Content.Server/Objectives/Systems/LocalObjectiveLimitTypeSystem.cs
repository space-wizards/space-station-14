using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

public sealed class LocalObjectiveLimitTypeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalObjectiveLimitTypeComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(Entity<LocalObjectiveLimitTypeComponent> entity, ref RequirementCheckEvent args)
    {

        if (args.Cancelled)
            return;

        var amtOfObjType = 0;
        foreach (var objective in args.Mind.Objectives)
        {
            if (entity.Owner == objective || !TryComp<LocalObjectiveLimitTypeComponent>(objective, out var locObjLimitTypeComp))
                continue;

            if (locObjLimitTypeComp.ObjectiveType == entity.Comp.ObjectiveType)
            {
                amtOfObjType++;
                if (amtOfObjType >= entity.Comp.Limit)
                {
                    args.Cancelled = true;
                    return;
                }
            }
        }
    }
}
