using Content.Server.Defusable.Components;
using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared.Administration.Logs;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Defusable;
using Content.Shared.Examine;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
namespace Content.Server.Defusable.Systems;
/// <inheritdoc/>
public sealed class DefusableSystem : SharedDefusableSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly WiresSystem _wiresSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
/// <inheritdoc/>
public override void Initialize()
{
    base.Initialize();

    SubscribeLocalEvent<DefusableComponent, ExaminedEvent>(OnExamine);
    SubscribeLocalEvent<DefusableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    SubscribeLocalEvent<DefusableComponent, AnchorAttemptEvent>(OnAnchorAttempt);
    SubscribeLocalEvent<DefusableComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
    SubscribeLocalEvent<DefusableComponent, TriggerEvent>(OnDefusableTriggered);
}

#region Subscribed Events
/// <summary>
///     Adds a verb allowing for the bomb to be started easily.
/// </summary>
    private void OnGetAltVerbs(EntityUid uid, DefusableComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Show remaining time as a disabled verb when active
        if (comp.Activated)
        {
            if (TryComp<TimerTriggerComponent>(uid, out var timerComp))
            {
                var remaining = _trigger.GetRemainingTime((uid, timerComp))?.TotalSeconds;
                if (remaining != null)
                {
                    var seconds = Math.Max(0, (int)Math.Ceiling(remaining.Value));
                    args.Verbs.Add(new AlternativeVerb
                    {
                        Category = TriggerSystem.TimerOptions,
                        Text = Loc.GetString("timer-trigger-verb-set-current", ("time", seconds)),
                        Disabled = true,
                        Priority = 1000
                    });
                }
            }
        }

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryStartCountdown(uid, args.User, comp),
            Text = Loc.GetString("Установить бомбу"),
            Priority = 1,
        });
    }

    private void OnExamine(EntityUid uid, DefusableComponent comp, ExaminedEvent args)
    {
        // Show activation / timer status
        if (comp.Activated)
        {
            // Remaining time if display is enabled and timer is active
            if (comp.DisplayTime && TryComp<TimerTriggerComponent>(uid, out var timer)
                && HasComp<ActiveTimerTriggerComponent>(uid))
            {
                var remaining = _trigger.GetRemainingTime((uid, timer));
                if (remaining != null)
                {
                    var seconds = Math.Max(0, (int)Math.Ceiling(remaining.Value.TotalSeconds));
                    args.PushText(Loc.GetString("defusable-examine-live", ("name", uid), ("time", seconds)));
                }
            }
            else
            {
                args.PushText(Loc.GetString("defusable-examine-live-display-off", ("name", uid)));
            }
        }
        else
        {
            args.PushText(Loc.GetString("defusable-examine-inactive", ("name", uid)));
        }

        // Bolts status
        args.PushText(Loc.GetString("defusable-examine-bolts", ("down", comp.Bolted)));
    }

    private void OnAnchorAttempt(EntityUid uid, DefusableComponent comp, AnchorAttemptEvent args)
    {
        // Allow anchoring by default.
    }

    private void OnUnanchorAttempt(EntityUid uid, DefusableComponent comp, UnanchorAttemptEvent args)
    {
        // Prevent unanchoring if bolted.
        if (comp.Bolted)
            args.Cancel();
    }

    // When the timer finishes and fires its key ('timer'), ensure announcement and round end happen.
    private void OnDefusableTriggered(EntityUid uid, DefusableComponent comp, ref TriggerEvent args)
    {
        // Only react to the timer output key
        if (args.Key != null && args.Key != "timer")
            return;

        _chatSystem.DispatchGlobalAnnouncement("Бомба взорвалась!", sender: "Центральное командование");
        if (_gameTicker.RunLevel == GameRunLevel.InRound)
            _gameTicker.EndRound("Бой фракций завершен. Победа за Террористами.");
    }

    private void TryStartCountdown(EntityUid uid, EntityUid user, DefusableComponent comp)
    {
        if (comp.Activated || !comp.Usable)
            return;

        var xform = Transform(uid);
        if (!xform.Anchored)
            _transform.AnchorEntity(uid, xform);
        SetBolt(comp, true);
        SetActivated(comp, true);

        _popup.PopupEntity(Loc.GetString("Установить", ("name", uid)), uid);

        if (TryComp<TimerTriggerComponent>(uid, out var timerTrigger))
        {
            _trigger.ActivateTimerTrigger((uid, timerTrigger));
        }

        // Global announcement (Central Command): bomb countdown has started
        _chatSystem.DispatchGlobalAnnouncement("Бомба установлена.");

        RaiseLocalEvent(uid, new BombArmedEvent(uid));

        _appearance.SetData(uid, DefusableVisuals.Active, comp.Activated);

        if (TryComp<WiresPanelComponent>(uid, out var wiresPanelComponent))
            _wiresSystem.TogglePanel(uid, wiresPanelComponent, false);
    }

    public void TryDetonateBomb(EntityUid uid, EntityUid detonator, DefusableComponent comp)
    {
        if (!comp.Activated)
            return;

        _popup.PopupEntity(Loc.GetString("defusable-popup-boom", ("name", uid)), uid, PopupType.LargeCaution);
        RaiseLocalEvent(uid, new BombDetonatedEvent(uid));
        _explosion.TriggerExplosive(uid, user: detonator);
        QueueDel(uid);

        // Global announcement (Central Command): bomb exploded, transition to PostRound
        _chatSystem.DispatchGlobalAnnouncement("Бомба взорвалась!", sender: "Центральное командование");
        if (_gameTicker.RunLevel == GameRunLevel.InRound)
            _gameTicker.EndRound("Бой фракций завершен. Победа за Террористами.");

        _appearance.SetData(uid, DefusableVisuals.Active, comp.Activated);
    }

    public void TryDefuseBomb(EntityUid uid, DefusableComponent comp)
    {
        if (!comp.Activated)
            return;

        _popup.PopupEntity(Loc.GetString("defusable-popup-defuse", ("name", uid)), uid);
        SetActivated(comp, false);

        var xform = Transform(uid);

        if (comp.Disposable)
        {
            SetUsable(comp, false);
            RemComp<ExplodeOnTriggerComponent>(uid);
            RemComp<TimerTriggerComponent>(uid);
        }
        RemComp<ActiveTimerTriggerComponent>(uid);

        _audio.PlayPvs(comp.DefusalSound, uid);

        RaiseLocalEvent(uid, new BombDefusedEvent(uid));

        comp.ActivatedWireUsed = false;
        comp.DelayWireUsed = false;
        comp.ProceedWireCut = false;
        comp.ProceedWireUsed = false;
        comp.Bolted = false;

        if (xform.Anchored)
            _transform.Unanchor(uid, xform);

        _appearance.SetData(uid, DefusableVisuals.Active, comp.Activated);
    }

    // jesus christ
    public void SetUsable(DefusableComponent component, bool value)
    {
        component.Usable = value;
    }

    public void SetDisplayTime(DefusableComponent component, bool value)
    {
        component.DisplayTime = value;
    }

    /// <summary>
    /// Sets the Activated value of a component to a value.
    /// </summary>
    /// <param name="component"></param>
    /// <param name="value"></param>
    /// <remarks>
    /// Use <see cref="TryDefuseBomb"/> to defuse bomb. This is a setter.
    /// </remarks>
    public void SetActivated(DefusableComponent component, bool value)
    {
        component.Activated = value;
    }

    public void SetBolt(DefusableComponent component, bool value)
    {
        component.Bolted = value;
    }

    #endregion

    #region Wires

    public void DelayWirePulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp is not { Activated: true, DelayWireUsed: false })
            return;

        _trigger.TryDelay(wire.Owner, TimeSpan.FromSeconds(30));
        _popup.PopupEntity(Loc.GetString("defusable-popup-wire-chirp", ("name", wire.Owner)), wire.Owner);
        comp.DelayWireUsed = true;
    }

    public bool ProceedWireCut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp is not { Activated: true, ProceedWireCut: false })
            return true;

        _popup.PopupEntity(Loc.GetString("defusable-popup-wire-proceed-pulse", ("name", wire.Owner)), wire.Owner);
        SetDisplayTime(comp, false);

        comp.ProceedWireCut = true;
        return true;
    }

    public void ProceedWirePulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp is { Activated: true, ProceedWireUsed: false })
        {
            comp.ProceedWireUsed = true;
            _trigger.TryDelay(wire.Owner, TimeSpan.FromSeconds(-15));
        }

        _popup.PopupEntity(Loc.GetString("defusable-popup-wire-proceed-pulse", ("name", wire.Owner)), wire.Owner);
    }

    public bool ActivateWireCut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        // if you cut the wire it just defuses the bomb

        if (comp.Activated)
        {
            TryDefuseBomb(wire.Owner, comp);

            _adminLogger.Add(LogType.Explosion, LogImpact.High,
                $"{ToPrettyString(user):user} has defused {ToPrettyString(wire.Owner):entity}!");
        }

        return true;
    }

    public void ActivateWirePulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        // if the component isnt active, just start the countdown
        // if it is and it isn't already used then delay it

        if (comp.Activated)
        {
            if (!comp.ActivatedWireUsed)
            {
                _trigger.TryDelay(wire.Owner, TimeSpan.FromSeconds(30));
                _popup.PopupEntity(Loc.GetString("defusable-popup-wire-chirp", ("name", wire.Owner)), wire.Owner);
                comp.ActivatedWireUsed = true;
            }
        }
        else
        {
            TryStartCountdown(wire.Owner, user, comp);
        }
    }

    public bool BoomWireCut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.Activated)
        {
            TryDetonateBomb(wire.Owner, user, comp);
        }
        else
        {
            SetUsable(comp, false);
        }
        return true;
    }

    public bool BoomWireMend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp is { Activated: false, Usable: false })
        {
            SetUsable(comp, true);
        }
        // you're already dead lol
        return true;
    }

    public void BoomWirePulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.Activated)
        {
            TryDetonateBomb(wire.Owner, user, comp);
        }
    }

    public bool BoltWireMend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (!comp.Activated)
            return true;

        SetBolt(comp, true);
        _audio.PlayPvs(comp.BoltSound, wire.Owner);
        _popup.PopupEntity(Loc.GetString("defusable-popup-wire-bolt-pulse", ("name", wire.Owner)), wire.Owner);

        return true;
    }

    public bool BoltWireCut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (!comp.Activated)
            return true;

        SetBolt(comp, false);
        _audio.PlayPvs(comp.BoltSound, wire.Owner);
        _popup.PopupEntity(Loc.GetString("defusable-popup-wire-bolt-pulse", ("name", wire.Owner)), wire.Owner);

        return true;
    }

    public void BoltWirePulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        _popup.PopupEntity(Loc.GetString("defusable-popup-wire-bolt-pulse", ("name", wire.Owner)), wire.Owner);
    }

    #endregion
}

public sealed class BombDefusedEvent : EntityEventArgs
{
    public EntityUid Entity;

    public BombDefusedEvent(EntityUid entity)
    {
        Entity = entity;
    }
}
public sealed class BombArmedEvent : EntityEventArgs
{
    public EntityUid Entity;

    public BombArmedEvent(EntityUid entity)
    {
        Entity = entity;
    }
}
public sealed class BombDetonatedEvent : EntityEventArgs
{
    public EntityUid Entity;

    public BombDetonatedEvent(EntityUid entity)
    {
        Entity = entity;
    }
}


