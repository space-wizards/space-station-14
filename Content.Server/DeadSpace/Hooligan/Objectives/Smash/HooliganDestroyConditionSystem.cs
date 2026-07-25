// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Objectives.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using System;
using Content.Shared.Tag;

namespace Content.Server.DeadSpace.Hooligan.Objectives;

public sealed class HooliganDestroyConditionSystem : EntitySystem
{
    
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        
        base.Initialize();

        SubscribeLocalEvent<HooliganSmashEvent>(OnSmash);
        SubscribeLocalEvent<HooliganDestroyConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

    }

    private void OnSmash(ref HooliganSmashEvent args)
    {
        if (!_mind.TryGetMind(args.Smasher, out var mindId, out var mind))
            return;

        foreach (var objId in mind.Objectives)
        {
            if (!TryComp<HooliganDestroyConditionComponent>(objId, out var condition))
                continue;
            if (!_tag.HasAnyTag(args.Target, condition.Targets))
                continue; 
            condition.Count++;
        }
    }
     private void OnGetProgress(Entity<HooliganDestroyConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryComp<NumberObjectiveComponent>(ent.Owner, out var number) || number.Target == 0)
        {
            args.Progress = 1f;
            return;
        }
        args.Progress = MathF.Min(ent.Comp.Count / (float) number.Target, 1f);
    }


}