using Content.Server.Power.Components;
using Content.Shared.Holopad;
using Content.Shared.Telephone;
using JetBrains.FormatRipper.Elf;
using Robust.Shared.Timing;
using System;

namespace Content.Server.Telephone;

public sealed class TelephoneSystem : SharedTelephoneSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelephoneComponent, TelephoneOutgoingCallEvent>(OnOutgoingCall);
        SubscribeLocalEvent<TelephoneComponent, TelephoneIncomingCallAttemptEvent>(OnIncomingCallAttempt);
        SubscribeLocalEvent<TelephoneComponent, TelephoneIncomingCallEvent>(OnIncomingCall);
        SubscribeLocalEvent<TelephoneComponent, TelephoneCallCommencedEvent>(OnCallCommenced);
        SubscribeLocalEvent<TelephoneComponent, TelephoneHungUpEvent>(OnHungUp);
        SubscribeLocalEvent<TelephoneComponent, TelephoneCallTerminatedEvent>(OnCallTerminated);

        SubscribeLocalEvent<TelephoneComponent, ComponentShutdown>(OnComponentShutdown);
    }

    #region: Events

    private void OnOutgoingCall(EntityUid uid, TelephoneComponent component, ref TelephoneOutgoingCallEvent ev)
    {
        component.LinkedTelephone = ev.RecipientTelephone;
        component.CurrentState = TelephoneState.Calling;
    }

    private void OnIncomingCallAttempt(EntityUid uid, TelephoneComponent component, ref TelephoneIncomingCallAttemptEvent ev)
    {
        if (!IsTelephoneReachable(uid, component))
        {
            ev.Cancelled = true;
            return;
        }
    }

    private void OnIncomingCall(EntityUid uid, TelephoneComponent component, ref TelephoneIncomingCallEvent ev)
    {
        component.User = null;
        component.LinkedTelephone = ev.CallingTelephone;
        component.CurrentState = TelephoneState.Ringing;
        component.StateStartTime = _timing.CurTick;
    }

    private void OnCallCommenced(EntityUid uid, TelephoneComponent component, ref TelephoneCallCommencedEvent ev)
    {
        component.CurrentState = TelephoneState.InCall;
        component.StateStartTime = _timing.CurTick;
    }

    private void OnHungUp(EntityUid uid, TelephoneComponent component, ref TelephoneHungUpEvent ev)
    {
        component.User = null;
        component.LinkedTelephone = null;
        component.CurrentState = TelephoneState.HangingUp;
        component.StateStartTime = _timing.CurTick;
    }

    private void OnCallTerminated(EntityUid uid, TelephoneComponent component, ref TelephoneCallTerminatedEvent ev)
    {
        component.User = null;
        component.LinkedTelephone = null;
        component.CurrentState = TelephoneState.Idle;
    }

    private void OnComponentShutdown(EntityUid uid, TelephoneComponent component, ref ComponentShutdown ev)
    {
        if (component.LinkedTelephone == null)
            return;

        var evHungUp = new TelephoneHungUpEvent(uid);
        RaiseLocalEvent(component.LinkedTelephone.Value, ref evHungUp);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityManager.EntityQueryEnumerator<TelephoneComponent>();
        while (query.MoveNext(out var ent, out var entTelephone))
        {
            switch (entTelephone.CurrentState)
            {
                case TelephoneState.Ringing:
                    if (_timing.CurTick.Value > entTelephone.StateStartTime.Value + entTelephone.RingingTimeout)
                    {
                        var evHungUp = new TelephoneHungUpEvent(ent);

                        // Trigger the linked telephone to also hang up
                        if (entTelephone.LinkedTelephone != null)
                            RaiseLocalEvent(entTelephone.LinkedTelephone.Value, ref evHungUp);

                        RaiseLocalEvent(ent, ref evHungUp);
                    }

                    break;

                case TelephoneState.HangingUp:
                    if (_timing.CurTick.Value > entTelephone.StateStartTime.Value + entTelephone.HangingUpTimeout)
                    {
                        var evCallTerminated = new TelephoneCallTerminatedEvent();
                        RaiseLocalEvent(ent, ref evCallTerminated);
                    }
                    break;
            }
        }
    }

    public void CallTelephone(EntityUid uid, TelephoneComponent component, EntityUid recipientTelephone, EntityUid caller)
    {
        if (!HasComp<TelephoneComponent>(recipientTelephone))
            return;

        var evCallAttempt = new TelephoneIncomingCallAttemptEvent(uid);
        RaiseLocalEvent(recipientTelephone, ref evCallAttempt);

        if (evCallAttempt.Cancelled)
        {
            var evHungUp = new TelephoneHungUpEvent(recipientTelephone);
            RaiseLocalEvent(uid, ref evHungUp);

            return;
        }

        component.User = caller;

        var evIncomingCall = new TelephoneIncomingCallEvent(uid);
        RaiseLocalEvent(recipientTelephone, ref evIncomingCall);

        var evOutgoingCall = new TelephoneOutgoingCallEvent(recipientTelephone);
        RaiseLocalEvent(recipientTelephone, ref evOutgoingCall);
    }

    public void AnswerTelephone(EntityUid uid, TelephoneComponent component, EntityUid callingTelephone, EntityUid recipient)
    {
        component.User = recipient;

        var evCallCommenced = new TelephoneCallCommencedEvent(callingTelephone, uid);

        RaiseLocalEvent(uid, ref evCallCommenced);
        RaiseLocalEvent(callingTelephone, ref evCallCommenced);
    }

    public void HangUpTelephone(EntityUid uid, TelephoneComponent component)
    {
        var evHungUp = new TelephoneHungUpEvent(uid);

        if (component.LinkedTelephone != null)
            RaiseLocalEvent(component.LinkedTelephone.Value, ref evHungUp);

        RaiseLocalEvent(uid, ref evHungUp);
    }

    public bool IsTelephoneReachable(EntityUid uid, TelephoneComponent component)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerReceiver) && !apcPowerReceiver.Powered)
            return false;

        if (IsTelephoneEngaged(uid, component))
            return false;

        return true;
    }

    public bool IsTelephoneEngaged(EntityUid uid, TelephoneComponent component)
    {
        return component.CurrentState != TelephoneState.Idle;
    }
}
