using Content.Server.Chemistry.EntitySystems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.DoAfter;
using Content.Shared.Extinguisher.Events;
using Content.Shared.Examine;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Extinguisher;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Extinguisher;

public sealed class FireExtinguisherSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireExtinguisherComponent, ComponentInit>(OnFireExtinguisherInit);
        SubscribeLocalEvent<FireExtinguisherComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<FireExtinguisherComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<FireExtinguisherComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<FireExtinguisherComponent, SprayAttemptEvent>(OnSprayAttempt);
        SubscribeLocalEvent<FireExtinguisherComponent, DoAfterAttemptEvent<CoolingDoAfterEvent>>(OnCoolUseAttempt);
        SubscribeLocalEvent<FireExtinguisherComponent, CoolingDoAfterEvent>(OnCoolDoAfter);
        SubscribeLocalEvent<FireExtinguisherComponent, ExaminedEvent>(OnExtinguisherExamined);
    }

    private void OnFireExtinguisherInit(EntityUid uid, FireExtinguisherComponent component, ComponentInit args)
    {
        if (component.HasSafety)
        {
            UpdateAppearance(uid, component);
        }
    }

    private void OnUseInHand(EntityUid uid, FireExtinguisherComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        ToggleSafety(uid, args.User, component);

        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, FireExtinguisherComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
        {
            return;
        }

        if (args.Handled)
            return;

        if (component.HasSafety && component.Safety)
        {
            _popupSystem.PopupEntity(Loc.GetString("fire-extinguisher-component-safety-on-message"), uid,
                args.User);
            return;
        }

        if (args.Target is not {Valid: true} target ||
            !_solutionContainerSystem.TryGetDrainableSolution(target, out var targetSolution) ||
            !_solutionContainerSystem.TryGetRefillableSolution(uid, out var container))
        {
            return;
        }

        args.Handled = true;

        var transfer = container.AvailableVolume;
        if (TryComp<SolutionTransferComponent>(uid, out var solTrans))
        {
            transfer = solTrans.TransferAmount;
        }
        transfer = FixedPoint2.Min(transfer, targetSolution.Volume);

        if (transfer > 0)
        {
            var drained = _solutionContainerSystem.Drain(target, targetSolution, transfer);
            _solutionContainerSystem.TryAddSolution(uid, container, drained);

            SoundSystem.Play(component.RefillSound.GetSound(), Filter.Pvs(uid), uid);
            _popupSystem.PopupEntity(Loc.GetString("fire-extinguisher-component-after-interact-refilled-message", ("owner", uid)),
                uid, args.Target.Value);
        }
    }

    private void OnGetInteractionVerbs(EntityUid uid, FireExtinguisherComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract)
            return;

        var verb = new InteractionVerb
        {
            Act = () => ToggleSafety(uid, args.User, component),
            Text = Loc.GetString("fire-extinguisher-component-verb-text"),
        };

        args.Verbs.Add(verb);
    }

    private void OnSprayAttempt(EntityUid uid, FireExtinguisherComponent component, SprayAttemptEvent args)
    {
        if (component.HasSafety && component.Safety)
        {
            _popupSystem.PopupEntity(Loc.GetString("fire-extinguisher-component-safety-on-message"), uid,
                args.User);
            args.Cancel();
        }
    }

    private void UpdateAppearance(EntityUid uid, FireExtinguisherComponent comp,
        AppearanceComponent? appearance=null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        if (comp.HasSafety)
        {
            _appearance.SetData(uid, FireExtinguisherVisuals.Safety, comp.Safety, appearance);
        }
    }

    public void ToggleSafety(EntityUid uid, EntityUid user,
        FireExtinguisherComponent? extinguisher = null)
    {
        if (!Resolve(uid, ref extinguisher))
            return;

        extinguisher.Safety = !extinguisher.Safety;
        SoundSystem.Play(extinguisher.SafetySound.GetSound(), Filter.Pvs(uid),
            uid, AudioHelpers.WithVariation(0.125f).WithVolume(-4f));
        UpdateAppearance(uid, extinguisher);
    }

    private void OnExtinguisherExamined(EntityUid uid, FireExtinguisherComponent comp, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("fire-extinguisher-component-on-examine-container-message", ("color", Color.Blue)));
    }

    #region GhettoChemistry
    private void OnCoolUseAttempt(EntityUid uid, FireExtinguisherComponent extinguisher, DoAfterAttemptEvent<CoolingDoAfterEvent> args)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, SprayComponent.SolutionName, out var solution))
        {
            args.Cancel();
        }

        var user = args.DoAfter.Args.User;
        if (extinguisher.HasSafety && extinguisher.Safety)
        {
            _popupSystem.PopupEntity(Loc.GetString("fire-extinguisher-component-safety-on-message"), uid, user);
            args.Cancel();
            return;
        }
        if (solution != null && solution.Volume <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("spray-component-is-empty-message"), uid, user);
            args.Cancel();
        }
    }

    private void OnCoolDoAfter(EntityUid uid, FireExtinguisherComponent extinguisher, CoolingDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_solutionContainerSystem.TryGetSolution(uid, SprayComponent.SolutionName, out var solution))
            return;

        solution.RemoveReagent(extinguisher.WaterReagent, FixedPoint2.New(args.Water));
        PlayToolSound(uid, args.User);
        if (args.Target != null)
        {
            _popupSystem.PopupEntity(Loc.GetString("fire-extinguisher-component-entity-message", ("entity", args.Target.Value)), uid, args.User);
        }
        var ev = args.WrappedEvent;
        ev.DoAfter = args.DoAfter;

        if (args.OriginalTarget != null)
            RaiseLocalEvent(args.OriginalTarget.Value, (object)ev);
        else
            RaiseLocalEvent((object)ev);
    }

    public (FixedPoint2 water, FixedPoint2 capacity) GetExtinguisherWaterAndCapacity(EntityUid uid, FireExtinguisherComponent? extinguisher = null, SolutionContainerManagerComponent? solutionContainer = null)
    {
        if (!Resolve(uid, ref extinguisher, ref solutionContainer)
            || !_solutionContainerSystem.TryGetSolution(uid, SprayComponent.SolutionName, out var waterSolution, solutionContainer))
            return (FixedPoint2.Zero, FixedPoint2.Zero);

        return (_solutionContainerSystem.GetReagentQuantity(uid, extinguisher.WaterReagent), waterSolution.MaxVolume);
    }

    public void PlayToolSound(EntityUid uid, EntityUid? user, SprayComponent? sprayComponent = null)
    {
        if (!Resolve(uid, ref sprayComponent))
            return;

        if (sprayComponent.SpraySound == null)
            return;

        _audioSystem.PlayPvs(sprayComponent.SpraySound, uid, sprayComponent.SpraySound.Params.WithVariation(0.125f));
    }

    public bool UseExtinguisher(EntityUid extinguisher, EntityUid user, EntityUid? target, float doAfterDelay, DoAfterEvent doAfterEv, float water = 0f, FireExtinguisherComponent? extinguisherComponent = null)
    {
        return UseExtinguisher(extinguisher, user, target, TimeSpan.FromSeconds(doAfterDelay), doAfterEv, out _, water, extinguisherComponent);
    }

    public bool UseExtinguisher(EntityUid extinguisher, EntityUid user, EntityUid? target, TimeSpan delay, DoAfterEvent doAfterEv, out DoAfterId? id, float water = 0f, FireExtinguisherComponent? extinguisherComponent = null)
    {
        id = null;
        if (!Resolve(extinguisher, ref extinguisherComponent, false))
            return false;

        var extinguisherEvent = new CoolingDoAfterEvent(water, doAfterEv, target);
        var doAfterArgs = new DoAfterArgs(user, delay, extinguisherEvent, extinguisher, target: target, used: extinguisher)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = extinguisher != user,
            AttemptFrequency = water <= 0 ? AttemptFrequency.Never : AttemptFrequency.EveryTick
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs, out id);
        return true;
    }
    #endregion GhettoChemistry

}
