// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Objectives.Components;
using Content.Shared.Mind;
using Content.Server.Objectives.Components;
using System;

namespace Content.Server.DeadSpace.Hooligan.Objectives;


/// <summary>
/// Логика для цели "Употребить". Считает прогресс и количество употреблённых наркотиков.
/// </summary>
public sealed class HooliganDrugConditionSystem : EntitySystem
{

    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HooliganDrugConsumedEvent>(OnDrugConsumed);
        SubscribeLocalEvent<HooliganDrugConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnDrugConsumed(ref HooliganDrugConsumedEvent args)
    {
        if (!_mind.TryGetObjectiveComp<HooliganDrugConditionComponent>(args.Body, out var condition))
            return;
        condition.Amount += args.Amount;
        
    }

    private void OnGetProgress(Entity<HooliganDrugConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryComp<NumberObjectiveComponent>(ent.Owner, out var number) || number.Target == 0)
        {
            args.Progress = 1f;
            return;
        }
        args.Progress = MathF.Min(ent.Comp.Amount.Float() / number.Target, 1f);
    }

    
}