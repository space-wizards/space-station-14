using Content.Server.Fluids.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Extinguisher;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Extinguisher;

public sealed class FireExtinguisherSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireExtinguisherComponent, ComponentInit>(OnFireExtinguisherInit);
        SubscribeLocalEvent<FireExtinguisherComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<FireExtinguisherComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<FireExtinguisherComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<FireExtinguisherComponent, SprayAttemptEvent>(OnSprayAttempt);
        /// <summary>
        /// Uses Content.Shared.Actions & Content.Shared.Toggleable to use code related to the Action menu. 
        /// This makes the component subscribe to events that will activate when the ActionToggleSafety prototype is clicked.
        /// </summary>
        SubscribeLocalEvent<FireExtinguisherComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<FireExtinguisherComponent, GetItemActionsEvent>(OnGetActions);
    }

    private void OnFireExtinguisherInit(EntityUid uid, FireExtinguisherComponent component, ComponentInit args)
    {
        if (component.HasSafety)
        {
            UpdateAppearance(uid, component);
            /// <summary>
            /// Initially sets Toggled to True so ActionUIController.cs uses "On sprite" via "action.iconOn"
            /// Action Menu will set Fire Extinguisher's "On sprite" by default instead of "Off sprite".
            /// </summary>
            _actions.SetToggled(component.ToggleActionEntity, false);
        }
    }

    private void OnUseInHand(EntityUid uid, FireExtinguisherComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        ToggleSafety(uid, component, args.User);

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

        if (args.Target is not { Valid: true } target ||
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

            _audio.PlayPvs(component.RefillSound, uid);
            _popupSystem.PopupEntity(Loc.GetString("fire-extinguisher-component-after-interact-refilled-message", ("owner", uid)),
                uid, args.Target.Value);
        }
    }
    /// <summary>
    /// Uses ActionContainerSystem.cs to check if the entity is already an action.
    /// Uses ActionEvents.cs to create the logic that registers the "ActionToggleSafety" entity in the Actions menu.  
    /// </summary>
    private void OnGetActions(EntityUid uid, FireExtinguisherComponent component, GetItemActionsEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }
    private void OnGetInteractionVerbs(EntityUid uid, FireExtinguisherComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract)
            return;

        var verb = new InteractionVerb
        {
            Act = () => ToggleSafety(uid, component, args.User),
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
    /// <summary>
    /// When the user clicks the entity defined by "ActionToggleSafety", a ToggleActionEvent
    /// recognizes this and activates OnToggleAction, which activates ToggleSafety.
    /// </summary>
    private void OnToggleAction(EntityUid uid, FireExtinguisherComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        ToggleSafety(uid, component, null);

        args.Handled = true;
    }
    private void UpdateAppearance(EntityUid uid, FireExtinguisherComponent comp,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        if (comp.HasSafety)
        {
            _appearance.SetData(uid, FireExtinguisherVisuals.Safety, comp.Safety, appearance);
        }
    }

    public void ToggleSafety(EntityUid uid,
        FireExtinguisherComponent? extinguisher = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref extinguisher))
            return;

        extinguisher.Safety = !extinguisher.Safety;
        _audio.PlayPvs(extinguisher.SafetySound, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(-4f));
        UpdateAppearance(uid, extinguisher);

        // Change the sprite from Off to On or On to Off via SharedActionSystem.cs & ActionUIController.cs StartTargeting()
        _actions.SetToggled(extinguisher.ToggleActionEntity, extinguisher.Safety);

    }
}
