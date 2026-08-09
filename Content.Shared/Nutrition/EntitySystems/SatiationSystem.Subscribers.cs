using Content.Shared.Actions.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;

namespace Content.Shared.Nutrition.EntitySystems;

public abstract partial class SatiationSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnActionAttempt(Entity<ActionRequireSatiationComponent> ent, ref ActionAttemptEvent args)
    {
        if (TryComp<SatiationComponent>(args.User, out var satiation)
            && GetValueOrNull((args.User, satiation), ent.Comp.Satiation) is {} value
            && value >= ent.Comp.Amount)
            return;

        if (ent.Comp.FailReason != null)
            args.Reason = Loc.GetString(ent.Comp.FailReason);

        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnActionPerformed(Entity<ActionRequireSatiationComponent> ent, ref ActionPerformedEvent args)
    {
        if (!ent.Comp.Spend)
            return;

        if (!TryComp<SatiationComponent>(args.Performer, out var satiation))
            return;

        ModifyValue((args.Performer, satiation), ent.Comp.Satiation, -ent.Comp.Amount.Float());
    }
}

