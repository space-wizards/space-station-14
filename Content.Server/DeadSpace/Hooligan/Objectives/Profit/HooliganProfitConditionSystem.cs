// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Objectives.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Roles;
using Content.Server.Backmen.Economy;
using Content.Shared.Backmen.Economy;
using Content.Shared.FixedPoint;
using System;

namespace Content.Server.DeadSpace.Hooligan.Objectives;

public sealed class HooliganProfitConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _role = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HooliganProfitConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

    }
    private void OnGetProgress(Entity<HooliganProfitConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryComp<NumberObjectiveComponent>(ent.Owner, out var number) || number.Target == 0)
        {
            args.Progress = 1f;
            return;
        }

        var balance = FixedPoint2.Zero;
        if (_role.MindHasRole<BankMemoryComponent>(args.MindId, out var bankRole) 
            && bankRole.Value.Comp2.BankAccount is {} account)
        {
            balance = account.Comp.Balance;
        }

        args.Progress = MathF.Min(balance.Float() / number.Target, 1f);
        
    }

}