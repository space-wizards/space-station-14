using Content.Shared.Administration.Logs;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Defusable.Components;
using Content.Shared.Examine;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Defusable.Systems;

public sealed class DefusableSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedWiresSystem _wiresSystem = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

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
    /// Adds a verb allowing for the bomb to be started easily.
    /// </summary>
    private void OnGetAltVerbs(Entity<DefusableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("defusable-verb-begin"),
            Disabled = ent.Comp is { Activated: true, Usable: true },
            Priority = 10,
            Act = () =>
            {
                TryStartCountdown(ent, user);
            }
        });
    }

    private void OnExamine(Entity<DefusableComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(DefusableComponent)))
        {
            if (!ent.Comp.Usable)
            {
                args.PushMarkup(Loc.GetString("defusable-examine-defused", ("name", ent.Owner)));
            }
            else if (ent.Comp.Activated)
            {
                var remaining = _trigger.GetRemainingTime(ent.Owner);
                if (ent.Comp.DisplayTime && remaining != null)
                {
                    args.PushMarkup(Loc.GetString("defusable-examine-live", ("name", ent.Owner),
                        ("time", Math.Floor(remaining.Value.TotalSeconds))));
                }
                else
                {
                    args.PushMarkup(Loc.GetString("defusable-examine-live-display-off", ("name", ent.Owner)));
                }
            }
            else
            {
                args.PushMarkup(Loc.GetString("defusable-examine-inactive", ("name", ent.Owner)));
            }
        }

        args.PushMarkup(Loc.GetString("defusable-examine-bolts", ("down", ent.Comp.Bolted)));
    }

    private void OnAnchorAttempt(Entity<DefusableComponent> ent, ref AnchorAttemptEvent args)
    {
        if (CheckAnchorAttempt(ent, args))
            args.Cancel();
    }

    private void OnUnanchorAttempt(Entity<DefusableComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (CheckAnchorAttempt(ent, args))
            args.Cancel();
    }

    private bool CheckAnchorAttempt(Entity<DefusableComponent> ent, BaseAnchoredAttemptEvent args)
    {
        // Don't allow the thing to be anchored if bolted to the ground.
        if (!ent.Comp.Bolted)
            return false;

        var msg = Loc.GetString("defusable-popup-cant-anchor", ("name", ent.Owner));
        _popup.PopupClient(msg, ent.Owner, args.User);

        return true;
    }

    #endregion

    #region Public

    public void TryStartCountdown(Entity<DefusableComponent> ent, EntityUid user)
    {
        if (!ent.Comp.Usable)
        {
            _popup.PopupPredicted(Loc.GetString("defusable-popup-fried", ("name", ent.Owner)), ent.Owner, user);
            return;
        }

        var xform = Transform(ent.Owner);
        if (!xform.Anchored)
            _transform.AnchorEntity(ent.Owner, xform);

        SetBolt(ent.Comp, true);
        SetActivated(ent.Comp, true);

        _popup.PopupPredicted(Loc.GetString("defusable-popup-begun", ("name", ent.Owner)), ent.Owner, user);
        if (TryComp<TimerTriggerComponent>(ent.Owner, out var timerTrigger))
        {
            _trigger.ActivateTimerTrigger((ent.Owner, timerTrigger));
        }

        RaiseLocalEvent(ent.Owner, new BombArmedEvent(ent.Owner));

        _appearance.SetData(ent.Owner, DefusableVisuals.Active, ent.Comp.Activated);

        if (TryComp<WiresPanelComponent>(ent.Owner, out var wiresPanelComponent))
            _wiresSystem.TogglePanel(ent.Owner, wiresPanelComponent, false);
    }

    public void TryDetonateBomb(Entity<DefusableComponent> ent, EntityUid detonator)
    {
        if (!ent.Comp.Activated)
            return;

        _popup.PopupPredicted(Loc.GetString("defusable-popup-boom", ("name", ent.Owner)), ent.Owner, ent.Owner, PopupType.LargeCaution);

        RaiseLocalEvent(ent.Owner, new BombDetonatedEvent(ent.Owner));

        _explosion.TriggerExplosive(ent.Owner, user: detonator);
        PredictedQueueDel(ent.Owner);

        _appearance.SetData(ent.Owner, DefusableVisuals.Active, ent.Comp.Activated);
    }

    public void TryDefuseBomb(Entity<DefusableComponent> ent)
    {
        if (!ent.Comp.Activated)
            return;

        _popup.PopupPredicted(Loc.GetString("defusable-popup-defuse", ("name", ent.Owner)), ent.Owner, ent.Owner);
        SetActivated(ent.Comp, false);

        var xform = Transform(ent.Owner);

        if (ent.Comp.Disposable)
        {
            SetUsable(ent.Comp, false);
            RemComp<ExplodeOnTriggerComponent>(ent.Owner);
            RemComp<TimerTriggerComponent>(ent.Owner);
        }
        RemComp<ActiveTimerTriggerComponent>(ent.Owner);

        _audio.PlayPredicted(ent.Comp.DefusalSound, ent.Owner, ent.Owner);

        RaiseLocalEvent(ent.Owner, new BombDefusedEvent(ent.Owner));

        ent.Comp.ActivatedWireUsed = false;
        ent.Comp.DelayWireUsed = false;
        ent.Comp.ProceedWireCut = false;
        ent.Comp.ProceedWireUsed = false;
        ent.Comp.Bolted = false;

        Dirty(ent);

        if (xform.Anchored)
            _transform.Unanchor(ent.Owner, xform);

        _appearance.SetData(ent.Owner, DefusableVisuals.Active, ent.Comp.Activated);
    }

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

    public void DelayWirePulse(Entity<DefusableComponent> ent, Wire wire)
    {
        if (ent.Comp is not { Activated: true, DelayWireUsed: false })
            return;

        _trigger.TryDelay(wire.Owner, TimeSpan.FromSeconds(30));
        _popup.PopupPredicted(Loc.GetString("defusable-popup-wire-chirp", ("name", wire.Owner)), wire.Owner, ent.Owner);
        ent.Comp.DelayWireUsed = true;
        Dirty(ent);
    }

    public bool ProceedWireCut(Entity<DefusableComponent> ent, Wire wire)
    {
        if (ent.Comp is not { Activated: true, ProceedWireCut: false })
            return true;

        _popup.PopupPredicted(Loc.GetString("defusable-popup-wire-proceed-pulse", ("name", wire.Owner)), wire.Owner, ent.Owner);
        SetDisplayTime(ent.Comp, false);

        ent.Comp.ProceedWireCut = true;
        Dirty(ent);

        return true;
    }

    public void ProceedWirePulse(Entity<DefusableComponent> ent, Wire wire)
    {
        if (ent.Comp is { Activated: true, ProceedWireUsed: false })
        {
            ent.Comp.ProceedWireUsed = true;
            Dirty(ent);
            _trigger.TryDelay(wire.Owner, TimeSpan.FromSeconds(-15));
        }

        _popup.PopupPredicted(Loc.GetString("defusable-popup-wire-proceed-pulse", ("name", wire.Owner)), wire.Owner, ent.Owner);
    }

    public bool ActivateWireCut(Entity<DefusableComponent> ent, Wire wire)
    {
        // If you cut the wire it just defuses the bomb.
        if (ent.Comp.Activated)
        {
            TryDefuseBomb((wire.Owner, ent.Comp));

            _adminLogger.Add(LogType.Explosion, LogImpact.High,
                $"{ToPrettyString(ent.Owner):user} has defused {ToPrettyString(wire.Owner):entity}!");
        }

        return true;
    }

    public void ActivateWirePulse(Entity<DefusableComponent> ent, Wire wire)
    {
        // If the component isn't active, just start the countdown.
        // If it is and it isn't already used then delay it.
        if (ent.Comp.Activated)
        {
            if (!ent.Comp.ActivatedWireUsed)
            {
                _trigger.TryDelay(wire.Owner, TimeSpan.FromSeconds(30));
                _popup.PopupPredicted(Loc.GetString("defusable-popup-wire-chirp", ("name", wire.Owner)), wire.Owner, ent.Owner);
                ent.Comp.ActivatedWireUsed = true;
                Dirty(ent);
            }
        }
        else
        {
            TryStartCountdown(ent, wire.Owner);
        }
    }

    public bool BoomWireCut(Entity<DefusableComponent> ent, Wire wire)
    {
        if (ent.Comp.Activated)
            TryDetonateBomb(ent, wire.Owner);
        else
            SetUsable(ent.Comp, false);

        return true;
    }

    public bool BoomWireMend(Entity<DefusableComponent> ent)
    {
        if (ent.Comp is { Activated: false, Usable: false })
            SetUsable(ent.Comp, true);

        return true;
    }

    public void BoomWirePulse(Entity<DefusableComponent> ent, Wire wire)
    {
        if (ent.Comp.Activated)
            TryDetonateBomb(ent, wire.Owner);
    }

    public bool BoltWireMend(Entity<DefusableComponent> ent, Wire wire)
    {
        if (!ent.Comp.Activated)
            return true;

        SetBolt(ent.Comp, true);
        _audio.PlayPredicted(ent.Comp.BoltSound, wire.Owner, ent.Owner);
        _popup.PopupPredicted(Loc.GetString("defusable-popup-wire-bolt-pulse", ("name", wire.Owner)), wire.Owner, ent.Owner);

        return true;
    }

    public bool BoltWireCut(Entity<DefusableComponent> ent, Wire wire)
    {
        if (!ent.Comp.Activated)
            return true;

        SetBolt(ent.Comp, false);
        _audio.PlayPredicted(ent.Comp.BoltSound, wire.Owner, ent.Owner);
        _popup.PopupPredicted(Loc.GetString("defusable-popup-wire-bolt-pulse", ("name", wire.Owner)), wire.Owner, ent.Owner);

        return true;
    }

    public void BoltWirePulse(Entity<DefusableComponent> ent, Wire wire)
    {
        _popup.PopupPredicted(Loc.GetString("defusable-popup-wire-bolt-pulse", ("name", wire.Owner)), wire.Owner, ent.Owner);
    }

    #endregion
}

public sealed class BombDefusedEvent(EntityUid entity) : EntityEventArgs
{
    public EntityUid Entity = entity;
}

public sealed class BombArmedEvent(EntityUid entity) : EntityEventArgs
{
    public EntityUid Entity = entity;
}

public sealed class BombDetonatedEvent(EntityUid entity) : EntityEventArgs
{
    public EntityUid Entity = entity;
}
