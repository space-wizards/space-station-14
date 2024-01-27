using Content.Shared.Actions.Events;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared.Actions;

/// <summary>
/// Handles action priming, confirmation and automatic unpriming.
/// </summary>
public sealed class ConfirmableActionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConfirmableActionComponent, ActionAttemptEvent>(OnAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // handle automatic unpriming
        var now = _timing.CurTime;
        foreach (var comp in EntityQuery<ConfirmableActionComponent>())
        {
            if (comp.NextUnprime is not {} time)
                continue;

            if (now >= time)
                Unprime(comp);
        }
    }

    private void OnAttempt(Entity<ConfirmableActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // if not primed, prime it and cancel the action
        if (ent.Comp.NextConfirm is not {} confirm)
        {
            Prime(ent.Comp, args.User);
            args.Cancelled = true;
            return;
        }

        // primed but the delay isnt over, cancel the action
        if (_timing.CurTime < confirm)
        {
            args.Cancelled = true;
            return;
        }

        // primed and delay has passed, let the action go through
        Unprime(ent.Comp);
    }

    private void Prime(ConfirmableActionComponent comp, EntityUid user)
    {
        comp.NextConfirm = _timing.CurTime + comp.ConfirmDelay;
        comp.NextUnprime = comp.NextConfirm + comp.PrimeTime;

        _popup.PopupClient(Loc.GetString(comp.Popup), user, user, PopupType.LargeCaution);
    }

    private void Unprime(ConfirmableActionComponent comp)
    {
        comp.NextConfirm = null;
        comp.NextUnprime = null;
    }
}
