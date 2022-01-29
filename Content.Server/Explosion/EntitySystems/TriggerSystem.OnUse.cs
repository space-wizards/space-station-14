using System;
using Content.Server.Explosion.Components;
using Content.Shared.Interaction;
using Content.Shared.Trigger;
using Robust.Shared.GameObjects;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void InitializeOnUse()
    {
        SubscribeLocalEvent<OnUseTimerTriggerComponent, UseInHandEvent>(OnTimerUse);
    }

    private void OnTimerUse(EntityUid uid, OnUseTimerTriggerComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        Trigger(uid, args.User, component);
        args.Handled = true;
    }

    // TODO: Need to split this out so it's a generic "OnUseTimerTrigger" component.
    private void Trigger(EntityUid uid, EntityUid user, OnUseTimerTriggerComponent component)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
            appearance.SetData(TriggerVisuals.VisualState, TriggerVisualState.Primed);

        HandleTimerTrigger(TimeSpan.FromSeconds(component.Delay), uid, user);
    }
}
