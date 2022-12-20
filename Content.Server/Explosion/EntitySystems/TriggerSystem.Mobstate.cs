using Content.Server.Explosion.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState;
using Robust.Shared.Player;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void InitializeMobstate()
    {
        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, SuicideEvent>(OnSuicide);
    }

    private void OnMobStateChanged(EntityUid uid, TriggerOnMobstateChangeComponent component, MobStateChangedEvent args)
    {
        if (component.MobState < args.CurrentMobState)
            return;

        //This chains Mobstate Changed triggers with OnUseTimerTrigger if they have it
        //Very useful for things that require a mobstate change and a timer
        if (TryComp<OnUseTimerTriggerComponent>(uid, out var timerTrigger))
        {
            HandleTimerTrigger(
                uid,
                args.Origin,
                timerTrigger.Delay,
                timerTrigger.BeepInterval,
                timerTrigger.InitialBeepDelay,
                timerTrigger.BeepSound,
                timerTrigger.BeepParams);
        }

        else
            Trigger(uid);
    }

    private void OnSuicide(EntityUid uid, TriggerOnMobstateChangeComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;

        if (component.PreventSuicide)
        {
            _popupSystem.PopupEntity(Loc.GetString("suicide-prevented"), args.Victim, args.Victim);
            args.BlockSuicideAttempt(component.PreventSuicide);
        }
    }
}
