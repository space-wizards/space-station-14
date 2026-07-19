using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Actions;

/// <summary>
/// Handles action priming, confirmation and automatic unpriming.
/// </summary>
public sealed partial class ConfirmableActionSystem : EntitySystem
{
    private static readonly EntityTimerId UnprimeTimer = new("unprime");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConfirmableActionComponent, ActionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ConfirmableActionComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ConfirmableActionComponent, EntityTimerEvent>(OnUnprimeTimer);
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
        _timers.SetTimerAt(ent, UnprimeTimer, comp.NextUnprime.Value);

        _popup.PopupEntity(Loc.GetString(comp.Popup), user, user, PopupType.LargeCaution);
    }

    private void Unprime(Entity<ConfirmableActionComponent> ent)
    {
        var (uid, comp) = ent;
        comp.NextConfirm = null;
        comp.NextUnprime = null;
        Dirty(uid, comp);
        _timers.CancelTimer<ConfirmableActionComponent>(uid, UnprimeTimer);
    }

    private void OnHandleState(Entity<ConfirmableActionComponent> ent, ref ComponentHandleState args)
    {
        if (ent.Comp.NextUnprime is {} deadline)
            _timers.SetTimerAt(ent, UnprimeTimer, deadline);
        else
            _timers.CancelTimer<ConfirmableActionComponent>(ent, UnprimeTimer);
    }

    private void OnUnprimeTimer(Entity<ConfirmableActionComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id == UnprimeTimer)
            Unprime(ent);
    }
}
