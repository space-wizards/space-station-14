using Content.Server.Defusable.Components;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared.Administration.Logs;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Defusable;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

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

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DefusableComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<DefusableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<DefusableComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<DefusableComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
    }

    #region Subscribed Events
    /// <summary>
    ///     Adds a verb allowing for the bomb to be started easily.
    /// </summary>
    private void OnGetAltVerbs(EntityUid uid, DefusableComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("defusable-verb-begin"),
            Disabled = comp is { Activated: true, Usable: true },
            Priority = 10,
            Act = () =>
            {
                TryStartCountdown(uid, args.User, comp);
            }
        });
    }

    private void OnExamine(EntityUid uid, DefusableComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!comp.Usable)
        {
            args.PushMarkup(Loc.GetString("defusable-examine-defused", ("name", uid)));
        }
        else if (comp.Activated && TryComp<ActiveTimerTriggerComponent>(uid, out var activeComp))
        {
            if (comp.DisplayTime)
            {
                args.PushMarkup(Loc.GetString("defusable-examine-live", ("name", uid),
                    ("time", MathF.Floor(activeComp.TimeRemaining))));
            }
            else
            {
                args.PushMarkup(Loc.GetString("defusable-examine-live-display-off", ("name", uid)));
            }
        }
        else
        {
            args.PushMarkup(Loc.GetString("defusable-examine-inactive", ("name", uid)));
        }

        args.PushMarkup(Loc.GetString("defusable-examine-bolts", ("down", comp.Bolted)));
    }

    private void OnAnchorAttempt(EntityUid uid, DefusableComponent component, AnchorAttemptEvent args)
    {
        if (CheckAnchorAttempt(uid, component, args))
            args.Cancel();
    }

    private void OnUnanchorAttempt(EntityUid uid, DefusableComponent component, UnanchorAttemptEvent args)
    {
        if (CheckAnchorAttempt(uid, component, args))
            args.Cancel();
    }

    private bool CheckAnchorAttempt(EntityUid uid, DefusableComponent component, BaseAnchoredAttemptEvent args)
    {
        // Don't allow the thing to be anchored if bolted to the ground
        if (!component.Bolted)
            return false;

        var msg = Loc.GetString("defusable-popup-cant-anchor", ("name", uid));
        _popup.PopupEntity(msg, uid, args.User);

        return true;
    }

    #endregion

    #region Public

    public void TryStartCountdown(EntityUid uid, EntityUid user, DefusableComponent comp)
    {
        if (!comp.Usable)
        {
            _popup.PopupEntity(Loc.GetString("defusable-popup-fried", ("name", uid)), uid);
            return;
        }

        var xform = Transform(uid);
        if (!xform.Anchored)
            _transform.AnchorEntity(uid, xform);

        SetBolt(comp, true);
        SetActivated(comp, true);

        _popup.PopupEntity(Loc.GetString("defusable-popup-begun", ("name", uid)), uid);
        if (TryComp<OnUseTimerTriggerComponent>(uid, out var timerTrigger))
        {
            _trigger.HandleTimerTrigger(
                uid,
                user,
                timerTrigger.Delay,
                timerTrigger.BeepInterval,
                timerTrigger.InitialBeepDelay,
                timerTrigger.BeepSound
            );
        }

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

        _explosion.TriggerExplosive(uid, user:detonator);
        QueueDel(uid);

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
            RemComp<OnUseTimerTriggerComponent>(uid);
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

        _trigger.TryDelay(wire.Owner, 30f);
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
            _trigger.TryDelay(wire.Owner, -15f);
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
                _trigger.TryDelay(wire.Owner, 30f);
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
