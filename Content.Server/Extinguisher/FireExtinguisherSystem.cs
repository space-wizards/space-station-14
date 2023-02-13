using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Extinguisher;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Extinguisher;

public sealed class FireExtinguisherSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireExtinguisherComponent, ComponentInit>(OnFireExtinguisherInit);
        SubscribeLocalEvent<FireExtinguisherComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<FireExtinguisherComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<FireExtinguisherComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<FireExtinguisherComponent, SprayAttemptEvent>(OnSprayAttempt);
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
}
