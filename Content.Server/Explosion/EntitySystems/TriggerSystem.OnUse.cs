using Content.Server.Explosion.Components;
using Content.Server.Sticky.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    private void InitializeOnUse()
    {
        SubscribeLocalEvent<OnUseTimerTriggerComponent, UseInHandEvent>(OnTimerUse);
        SubscribeLocalEvent<OnUseTimerTriggerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<OnUseTimerTriggerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<OnUseTimerTriggerComponent, EntityStuckEvent>(OnStuck);
    }

    private void OnStuck(EntityUid uid, OnUseTimerTriggerComponent component, EntityStuckEvent args)
    {
        if (!component.StartOnStick)
            return;

        HandleTimerTrigger(
            uid,
            args.User,
            component.Delay,
            component.BeepInterval,
            component.InitialBeepDelay,
            component.BeepSound);
    }

    private void OnExamined(EntityUid uid, OnUseTimerTriggerComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && component.Examinable)
            args.PushText(Loc.GetString("examine-trigger-timer", ("time", component.Delay)));
    }

    /// <summary>
    ///     Add an alt-click interaction that cycles through delays.
    /// </summary>
    private void OnGetAltVerbs(EntityUid uid, OnUseTimerTriggerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (component.UseVerbInstead)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("verb-start-detonation"),
                Act = () => HandleTimerTrigger(
                    uid,
                    args.User,
                    component.Delay,
                    component.BeepInterval,
                    component.InitialBeepDelay,
                    component.BeepSound
                ),
                Priority = 2
            });
        }

        if (component.AllowToggleStartOnStick)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("verb-toggle-start-on-stick"),
                Act = () => ToggleStartOnStick(uid, args.User, component)
            });
        }

        if (component.DelayOptions == null || component.DelayOptions.Count == 1)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Category = TimerOptions,
            Text = Loc.GetString("verb-trigger-timer-cycle"),
            Act = () => CycleDelay(component, args.User),
            Priority = 1
        });

        foreach (var option in component.DelayOptions)
        {
            if (MathHelper.CloseTo(option, component.Delay))
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Category = TimerOptions,
                    Text = Loc.GetString("verb-trigger-timer-set-current", ("time", option)),
                    Disabled = true,
                    Priority = (int) (-100 * option)
                });
                continue;
            }

            args.Verbs.Add(new AlternativeVerb()
            {
                Category = TimerOptions,
                Text = Loc.GetString("verb-trigger-timer-set", ("time", option)),
                Priority = (int) (-100 * option),

                Act = () =>
                {
                    component.Delay = option;
                    _popupSystem.PopupEntity(Loc.GetString("popup-trigger-timer-set", ("time", option)), args.User, args.User);
                },
            });
        }
    }

    private void CycleDelay(OnUseTimerTriggerComponent component, EntityUid user)
    {
        if (component.DelayOptions == null || component.DelayOptions.Count == 1)
            return;

        // This is somewhat inefficient, but its good enough. This is run rarely, and the lists should be short.

        component.DelayOptions.Sort();

        if (component.DelayOptions[^1] <= component.Delay)
        {
            component.Delay = component.DelayOptions[0];
            _popupSystem.PopupEntity(Loc.GetString("popup-trigger-timer-set", ("time", component.Delay)), user, user);
            return;
        }

        foreach (var option in component.DelayOptions)
        {
            if (option > component.Delay)
            {
                component.Delay = option;
                _popupSystem.PopupEntity(Loc.GetString("popup-trigger-timer-set", ("time", option)), user, user);
                return;
            }
        }
    }

    private void ToggleStartOnStick(EntityUid grenade, EntityUid user, OnUseTimerTriggerComponent comp)
    {
        if (comp.StartOnStick)
        {
            comp.StartOnStick = false;
            _popupSystem.PopupEntity(Loc.GetString("popup-start-on-stick-off"), grenade, user);
        }
        else
        {
            comp.StartOnStick = true;
            _popupSystem.PopupEntity(Loc.GetString("popup-start-on-stick-on"), grenade, user);
        }
    }

    private void OnTimerUse(EntityUid uid, OnUseTimerTriggerComponent component, UseInHandEvent args)
    {
        if (args.Handled || HasComp<AutomatedTimerComponent>(uid) || component.UseVerbInstead)
            return;

        HandleTimerTrigger(
            uid,
            args.User,
            component.Delay,
            component.BeepInterval,
            component.InitialBeepDelay,
            component.BeepSound);

        args.Handled = true;
    }

    public static VerbCategory TimerOptions = new("verb-categories-timer", "/Textures/Interface/VerbIcons/clock.svg.192dpi.png");
}
