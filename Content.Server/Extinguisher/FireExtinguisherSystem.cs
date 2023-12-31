using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Extinguisher;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Extinguisher;

public sealed class FireExtinguisherSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireExtinguisherComponent, ComponentInit>(OnFireExtinguisherInit);
        SubscribeLocalEvent<FireExtinguisherComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<FireExtinguisherComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<FireExtinguisherComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<FireExtinguisherComponent, SprayAttemptEvent>(OnSprayAttempt);
    }

    private void OnFireExtinguisherInit(Entity<FireExtinguisherComponent> entity, ref ComponentInit args)
    {
        if (entity.Comp.HasSafety)
        {
            UpdateAppearance((entity.Owner, entity.Comp));
        }
    }

    private void OnUseInHand(Entity<FireExtinguisherComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        ToggleSafety((entity.Owner, entity.Comp), args.User);

        args.Handled = true;
    }

    private void OnAfterInteract(Entity<FireExtinguisherComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
        {
            return;
        }

        if (args.Handled)
            return;

        if (entity.Comp.HasSafety && entity.Comp.Safety)
        {
            _popupSystem.PopupEntity(Loc.GetString("fire-extinguisher-component-safety-on-message"), entity.Owner, args.User);
            return;
        }

        if (args.Target is not { Valid: true } target ||
            !_solutionContainerSystem.TryGetDrainableSolution(target, out var targetSoln, out var targetSolution) ||
            !_solutionContainerSystem.TryGetRefillableSolution(entity.Owner, out var containerSoln, out var containerSolution))
        {
            return;
        }

        args.Handled = true;

        var transfer = containerSolution.AvailableVolume;
        if (TryComp<SolutionTransferComponent>(entity.Owner, out var solTrans))
        {
            transfer = solTrans.TransferAmount;
        }
        transfer = FixedPoint2.Min(transfer, targetSolution.Volume);

        if (transfer > 0)
        {
            var drained = _solutionContainerSystem.Drain(target, targetSoln.Value, transfer);
            _solutionContainerSystem.TryAddSolution(containerSoln.Value, drained);

            _audio.PlayPvs(entity.Comp.RefillSound, entity.Owner);
            _popupSystem.PopupEntity(Loc.GetString("fire-extinguisher-component-after-interact-refilled-message", ("owner", entity.Owner)),
                entity.Owner, args.Target.Value);
        }
    }

    private void OnGetInteractionVerbs(Entity<FireExtinguisherComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract)
            return;

        var user = args.User;
        var verb = new InteractionVerb
        {
            Act = () => ToggleSafety((entity.Owner, entity.Comp), user),
            Text = Loc.GetString("fire-extinguisher-component-verb-text"),
        };

        args.Verbs.Add(verb);
    }

    private void OnSprayAttempt(Entity<FireExtinguisherComponent> entity, ref SprayAttemptEvent args)
    {
        if (entity.Comp.HasSafety && entity.Comp.Safety)
        {
            _popupSystem.PopupEntity(Loc.GetString("fire-extinguisher-component-safety-on-message"), entity, args.User);
            args.Cancel();
        }
    }

    private void UpdateAppearance(Entity<FireExtinguisherComponent, AppearanceComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2, false))
            return;

        if (entity.Comp1.HasSafety)
        {
            _appearance.SetData(entity, FireExtinguisherVisuals.Safety, entity.Comp1.Safety, entity.Comp2);
        }
    }

    public void ToggleSafety(Entity<FireExtinguisherComponent?> extinguisher, EntityUid user)
    {
        if (!Resolve(extinguisher, ref extinguisher.Comp))
            return;

        extinguisher.Comp.Safety = !extinguisher.Comp.Safety;
        _audio.PlayPvs(extinguisher.Comp.SafetySound, extinguisher, AudioParams.Default.WithVariation(0.125f).WithVolume(-4f));
        UpdateAppearance((extinguisher.Owner, extinguisher.Comp));
    }
}
