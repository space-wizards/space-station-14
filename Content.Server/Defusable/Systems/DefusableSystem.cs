using Content.Server.Construction;
using Content.Server.Defusable.Components;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Salvage;
using Content.Shared.Construction.Components;
using Content.Shared.Defusable;
using Content.Shared.Examine;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Defusable.Systems;

/// <inheritdoc/>
public sealed class DefusableSystem : SharedDefusableSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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
        if (!args.CanInteract || !args.CanAccess)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("defusable-verb-begin"),
            Disabled = comp.BombLive && comp.BombUsable,
            Priority = 10,
            Act = () =>
            {
                TryStartCountdown(uid, comp);
            }
        });
    }

    private void OnExamine(EntityUid uid, DefusableComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!comp.BombUsable)
        {
            args.PushMarkup(Loc.GetString("defusable-examine-defused", ("name", uid)));
        }
        else if (comp.BombLive && TryComp<ActiveTimerTriggerComponent>(uid, out var activeComp))
        {
            if (comp.BombDisplayTime)
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
    }

    #endregion

    #region Anchorable

    private void OnAnchorAttempt(EntityUid uid, DefusableComponent component, AnchorAttemptEvent args)
    {
        CheckAnchorAttempt(uid, component, args);
    }

    private void OnUnanchorAttempt(EntityUid uid, DefusableComponent component, UnanchorAttemptEvent args)
    {
        CheckAnchorAttempt(uid, component, args);
    }

    private void CheckAnchorAttempt(EntityUid uid, DefusableComponent component, BaseAnchoredAttemptEvent args)
    {
        // Don't allow the thing to be anchored if bolted
        if (component.Bolted)
        {
            var msg = Loc.GetString("defusable-popup-cant-anchor");
            _popup.PopupEntity(msg, uid, args.User);

            args.Cancel();
        }
    }
    #endregion


    #region Public

    public void TryDelay(EntityUid uid, float amount, ActiveTimerTriggerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        comp.TimeRemaining += amount;
    }

    public void TryStartCountdown(EntityUid uid, DefusableComponent comp)
    {
        if (!comp.BombUsable)
        {
            _popup.PopupEntity(Loc.GetString("defusable-popup-fried", ("name", uid)), uid);
            return;
        }

        var xform = Transform(uid);
        xform.Anchored = true;
        comp.Bolted = true;

        comp.BombLive = true;
        _popup.PopupEntity(Loc.GetString("defusable-popup-begun", ("name", uid)), uid);
        if (TryComp<OnUseTimerTriggerComponent>(uid, out var timerTrigger))
        {
            _trigger.HandleTimerTrigger(
                uid,
                null,
                timerTrigger.Delay,
                timerTrigger.BeepInterval,
                timerTrigger.InitialBeepDelay,
                timerTrigger.BeepSound,
                timerTrigger.BeepParams
            );
        }

        Logger.Debug("it begins");

        UpdateAppearance(uid, comp);
    }

    public void TryDetonateBomb(EntityUid uid, DefusableComponent comp)
    {
        // also might want to have admin logs
        if (!comp.BombLive)
            return;

        _popup.PopupEntity(Loc.GetString("defusable-popup-boom", ("name", uid)), uid, PopupType.LargeCaution);

        _explosion.TriggerExplosive(uid);
        QueueDel(uid);

        UpdateAppearance(uid, comp);
    }

    public void TryDefuseBomb(EntityUid uid, DefusableComponent comp)
    {
        if (!comp.BombLive)
            return;

        _popup.PopupEntity(Loc.GetString("defusable-popup-defuse", ("name", uid)), uid);
        comp.BombLive = false;
        comp.BombUsable = false; // fry the circuitry

        if (TryComp<ExplodeOnTriggerComponent>(uid, out var explodeComp))
            RemComp<ExplodeOnTriggerComponent>(uid);

        if (TryComp<ActiveTimerTriggerComponent>(uid, out var activeComp))
            RemComp<ActiveTimerTriggerComponent>(uid);

        if (TryComp<OnUseTimerTriggerComponent>(uid, out var timerComp))
            RemComp<OnUseTimerTriggerComponent>(uid);

        _audio.PlayPvs(comp.DefusalSound, uid);

        UpdateAppearance(uid, comp);
    }

    #endregion

    private void UpdateAppearance(EntityUid uid, DefusableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        _appearance.SetData(uid, DefusableVisuals.Active, comp.BombLive);
    }
}
