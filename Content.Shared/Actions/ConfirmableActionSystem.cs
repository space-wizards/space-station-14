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
        var query = EntityQueryEnumerator<ConfirmableActionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextUnprime is not {} time)
                continue;

            if (now >= time)
                Unprime((uid, comp));
        }
    }

    private void OnAttempt(Entity<ConfirmableActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // if not primed, prime it and cancel the action
        if (ent.Comp.NextConfirm is not {} confirm)
        {
            Prime(ent, args.User);
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
        Unprime(ent);
    }

    private void Prime(Entity<ConfirmableActionComponent> ent, EntityUid user)
    {
        var (uid, comp) = ent;
        comp.NextConfirm = _timing.CurTime + comp.ConfirmDelay;
        comp.NextUnprime = comp.NextConfirm + comp.PrimeTime;
        Dirty(uid, comp);

        _popup.PopupClient(Loc.GetString(comp.Popup), user, user, PopupType.LargeCaution);
    }

    private void Unprime(Entity<ConfirmableActionComponent> ent)
    {
        var (uid, comp) = ent;
        comp.NextConfirm = null;
        comp.NextUnprime = null;
        Dirty(uid, comp);
    }
}
