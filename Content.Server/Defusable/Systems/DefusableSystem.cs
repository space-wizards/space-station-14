using Content.Server.Defusable.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Salvage;
using Content.Shared.Defusable;
using Content.Shared.Examine;
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

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DefusableComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<DefusableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    /// <summary>
    ///     Add an alt-click interaction that cycles through delays.
    /// </summary>
    private void OnGetAltVerbs(EntityUid uid, DefusableComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("defusable-verb-begin"),
            Disabled = false,
            Priority = 10,
            Act = () =>
            {
                TryStartCountdown(uid, comp);
            }
        });

        if (comp.StartingTimeOptions == null || comp.StartingTimeOptions.Count == 1)
            return;


        foreach (var option in comp.StartingTimeOptions)
        {
            if (MathHelper.CloseTo(option, comp.StartingTime))
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
                    comp.StartingTime = option;
                    _popup.PopupEntity(Loc.GetString("popup-trigger-timer-set", ("time", option)), args.User,
                        args.User);
                },
            });
        }
    }

    private void OnExamine(EntityUid uid, DefusableComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!comp.BombUsable) {
            args.PushMarkup(Loc.GetString("defusable-examine-defused", ("name", uid)));
        }
        else if (comp.BombLive)
        {
            args.PushMarkup(Loc.GetString("defusable-examine-live", ("name", uid), ("time", comp.TimeUntilExplosion.ToString())));
        }
        else
        {
            args.PushMarkup(Loc.GetString("defusable-examine-inactive", ("name", uid)));
        }
    }

    public void TryStartCountdown(EntityUid uid, DefusableComponent comp)
    {
        if (!comp.BombUsable)
            return;

        comp.BombLive = true;
        comp.TimeUntilExplosion = comp.StartingTime;
        _popup.PopupEntity(Loc.GetString("defusable-popup-begun", ("name", uid)), uid);
        AddComp<ActiveDefusableComponent>(uid);

        Logger.Debug("it begins");

        UpdateAppearance(uid, comp);
    }

    public void TryDetonateBomb(EntityUid uid, DefusableComponent comp)
    {
        // todo: boom??? lol?
        // also might want to have admin logs
        if (!comp.BombLive)
            return;

        _popup.PopupEntity(Loc.GetString("defusable-popup-boom", ("name", uid)), uid);

        _explosion.TriggerExplosive(uid);
        RemComp<ActiveDefusableComponent>(uid);
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
        RemComp<ActiveDefusableComponent>(uid);

        UpdateAppearance(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var activeComp in EntityQuery<ActiveDefusableComponent>())
        {
            if (!EntityManager.TryGetComponent<DefusableComponent>(activeComp.Owner, out var comp))
                continue;

            comp.TimeUntilExplosion -= frameTime;
            comp.TimeUntilNextBeep -= frameTime;

            Logger.Debug("woo!");

            if (comp.TimeUntilExplosion <= 0)
            {
                // i don't know any other way
                TryDetonateBomb(comp.Owner, comp);
                continue;
            }

            if (comp.BeepSound == null || comp.TimeUntilNextBeep > 0)
                continue;

            comp.TimeUntilNextBeep += comp.BeepInterval;
            var filter = Filter.Pvs(comp.Owner, entityManager: EntityManager);
            SoundSystem.Play(comp.BeepSound.GetSound(), filter, comp.Owner, comp.BeepParams);
        }
    }

    private void UpdateAppearance(EntityUid uid, DefusableComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        // _appearance.SetData(uid, DefusableVisuals.Active, comp.Wires == MagnetStateType.Attaching);
        // _appearance.SetData(uid, DefusableVisuals.ActiveWires, comp.Wires == MagnetStateType.Holding);
        // _appearance.SetData(uid, DefusableVisuals.Inactive, comp.Wires == MagnetStateType.CoolingDown);
        // _appearance.SetData(uid, DefusableVisuals.InactiveWires, comp.Wires == MagnetStateType.Detaching);
    }

    public static VerbCategory TimerOptions = new("verb-categories-timer", "/Textures/Interface/VerbIcons/clock.svg.192dpi.png");
}
