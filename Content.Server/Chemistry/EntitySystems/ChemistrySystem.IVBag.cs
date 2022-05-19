using System.Threading;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.CombatMode;
using Content.Server.DoAfter;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem
{
    private void InitializeIVBag()
    {
        SubscribeLocalEvent<IVBagComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<IVBagComponent, HandDeselectedEvent>(OnInjectorDeselected);
        SubscribeLocalEvent<IVBagComponent, ComponentStartup>(OnInjectorStartup);
        SubscribeLocalEvent<IVBagComponent, UseInHandEvent>(OnInjectorUse);
        SubscribeLocalEvent<IVBagComponent, AfterInteractEvent>(OnInjectorAfterInteract);
        SubscribeLocalEvent<IVBagComponent, ComponentGetState>(OnInjectorGetState);
        SubscribeLocalEvent<IVBagComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<IVBagComponent, ActivateInWorldEvent>(OnWorldActivate);

        SubscribeLocalEvent<BagInjectionCompleteEvent>(OnBagInjectionComplete);
        SubscribeLocalEvent<BagInjectionCancelledEvent>(OnBagInjectionCancelled);

    }

    private static void OnBagInjectionCancelled(BagInjectionCancelledEvent ev)
    {
        ev.Component.InjectCancel = null;
    }

    private void OnBagInjectionComplete(BagInjectionCompleteEvent ev)
    {
        ev.Component.InjectCancel = null;
        UseInjector(ev.Target, ev.User, ev.Component);
    }

    private static void OnInjectorDeselected(EntityUid uid, IVBagComponent component, HandDeselectedEvent args)
    {
        component.InjectCancel?.Cancel();
        component.InjectCancel = null;
    }

    private void OnInjectorStartup(EntityUid uid, IVBagComponent component, ComponentStartup args)
    {
        Dirty(component);
    }

    private void OnInjectorUse(EntityUid uid, IVBagComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        Toggle(component, args.User);
        args.Handled = true;
    }

    private void OnWorldActivate(EntityUid uid, IVBagComponent bagComp, ActivateInWorldEvent args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        Toggle(bagComp, args.User);
        args.Handled = true;
    }

    private void OnExamined(EntityUid uid, IVBagComponent bagComp, ExaminedEvent args)
    {
        string modeLabel;

        switch (bagComp.ToggleState)
        {
            case SharedIVBagComponent.IVBagToggleMode.Inject:
                modeLabel = "injector-inject-text";
                break;
            case SharedIVBagComponent.IVBagToggleMode.Draw:
                modeLabel = "injector-draw-text";
                break;
            case SharedIVBagComponent.IVBagToggleMode.Closed:
                modeLabel = "ivbag-closed-text";
                break;
            default:
                modeLabel = "injector-invalid-injector-toggle-mode";
                throw new ArgumentOutOfRangeException();
        }

        args.PushMarkup(Loc.GetString("ivbag-state-examine",
            ("mode", Loc.GetString(modeLabel))));

        if (bagComp.Connected && bagComp.Mob is { Valid: true } mob)
            args.PushMarkup(Loc.GetString("ivbag-connected-examine",
                ("color", Color.White.ToHexNoAlpha()),
                ("target", mob)));
    }

    /// <summary>
    /// Toggle between inject/draw/closed states.
    /// </summary>
    private void Toggle(IVBagComponent bagComp, EntityUid user)
    {
        string msg;
        switch (bagComp.ToggleState)
        {
            case SharedIVBagComponent.IVBagToggleMode.Inject:
                bagComp.ToggleState = SharedIVBagComponent.IVBagToggleMode.Draw;
                msg = "ivbag-component-drawing-text";
                break;
            case SharedIVBagComponent.IVBagToggleMode.Draw:
                bagComp.ToggleState = SharedIVBagComponent.IVBagToggleMode.Closed;
                msg = "ivbag-component-closed-text";
                break;
            case SharedIVBagComponent.IVBagToggleMode.Closed:
                bagComp.ToggleState = SharedIVBagComponent.IVBagToggleMode.Inject;
                msg = "ivbag-component-injecting-text";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Reset the flow timer after toggling.
        if (bagComp.Mob is { Valid: true } mob)
        {
            Disconnect(bagComp, false);
            Connect(bagComp, mob, user);
        }

        _popup.PopupEntity(Loc.GetString(msg), bagComp.Owner, Filter.Entities(user));
    }

    private bool UseInjector(EntityUid target, EntityUid user, IVBagComponent component)
    {
        // Halt any existing flows.
        if (component.Connected)
            Disconnect(component);

        // Handle injecting/drawing for solutions
        if (component.ToggleState == SharedIVBagComponent.IVBagToggleMode.Inject)
        {
            if (HasComp<BloodstreamComponent>(target))
            {
                Connect(component, target, user);
            }
            else if (_solutions.TryGetRefillableSolution(target, out var refillableSolution))
            {
                TryInject(component, target, refillableSolution, user, true);
            }
            else if (_solutions.TryGetInjectableSolution(target, out var injectableSolution))
            {
                TryInject(component, target, injectableSolution, user, false);
            }
            else
            {
                // _popup.PopupEntity(Loc.GetString("injector-component-cannot-transfer-message",
                    // ("target", target)), component.Owner, Filter.Entities(user));
                return false;
            }
        }
        else if (component.ToggleState == SharedIVBagComponent.IVBagToggleMode.Draw)
        {
            if (HasComp<BloodstreamComponent>(target))
            {
                Connect(component, target, user);
            }
            else if (_solutions.TryGetDrawableSolution(target, out var drawableSolution))
            {
                TryDraw(component, target, drawableSolution, user);
            }
            else
            {
                // _popup.PopupEntity(Loc.GetString("injector-component-cannot-draw-message",
                    // ("target", target)), component.Owner, Filter.Entities(user));
                return false;
            }
        }
        else if (component.ToggleState == SharedIVBagComponent.IVBagToggleMode.Closed)
        {
            if (HasComp<BloodstreamComponent>(target))
            {
                Connect(component, target, user);
            }
        }

        return true;
    }

    private void OnSolutionChange(EntityUid uid, IVBagComponent component, SolutionChangedEvent args)
    {
        Dirty(component);
    }

    private void OnInjectorGetState(EntityUid uid, IVBagComponent component, ref ComponentGetState args)
    {
        _solutions.TryGetSolution(uid, IVBagComponent.SolutionName, out var solution);

        var currentVolume = solution?.CurrentVolume ?? FixedPoint2.Zero;
        var maxVolume = solution?.MaxVolume ?? FixedPoint2.Zero;

        args.State = new SharedIVBagComponent.IVBagComponentState(currentVolume, maxVolume, component.ToggleState, component.Connected);
    }

    private void OnInjectorAfterInteract(EntityUid uid, IVBagComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach) return;

        if (component.InjectCancel != null)
        {
            args.Handled = true;
            return;
        }

        //Make sure we have the attacking entity
        if (args.Target is not { Valid: true } target ||
            !HasComp<SolutionContainerManagerComponent>(uid))
        {
            return;
        }

        // Is the target a mob? If yes, use a do-after to give them time to respond.
        if (HasComp<MobStateComponent>(target) ||
            HasComp<BloodstreamComponent>(target))
        {
            InjectDoAfter(component, args.User, target);
            args.Handled = true;
            return;
        }

        // Don't override default behaviors (like table placing) if injection failed.
        args.Handled = UseInjector(target, args.User, component);
    }

    /// <summary>
    /// Send informative pop-up messages and wait for a do-after to complete.
    /// </summary>
    private void InjectDoAfter(IVBagComponent component, EntityUid user, EntityUid target)
    {
        // Create a pop-up for the user
        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, Filter.Entities(user));

        if (!_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var solution))
            return;

        var actualDelay = MathF.Max(component.InjectDelay, 1f);
        if (user != target)
        {
            // Create a pop-up for the target
            var userName = MetaData(user).EntityName;
            _popup.PopupEntity(Loc.GetString("injector-component-injecting-target",
                ("user", userName)), user, Filter.Entities(target));

            // Check if the target is incapacitated or in combat mode and modify time accordingly.
            if (TryComp<MobStateComponent>(target, out var mobState) && mobState.IsIncapacitated())
            {
                actualDelay /= 2;
            }
            else if (TryComp<CombatModeComponent>(target, out var combat) && combat.IsInCombatMode)
            {
                // Slightly increase the delay when the target is in combat mode. Helps prevents cheese injections in
                // combat with fast syringes & lag.
                actualDelay += 1;
            }

            // Add an admin log, using the "force feed" log type. It's not quite feeding, but the effect is the same.
            if (component.ToggleState == SharedIVBagComponent.IVBagToggleMode.Inject)
            {
                _logs.Add(LogType.ForceFeed,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to inject {EntityManager.ToPrettyString(target):target} with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}");
            }
        }
        else
        {
            // Self-injections take half as long.
            actualDelay /= 2;

            if (component.ToggleState == SharedIVBagComponent.IVBagToggleMode.Inject)
                _logs.Add(LogType.Ingestion,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to inject themselves with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}.");
        }

        component.InjectCancel = new CancellationTokenSource();

        _doAfter.DoAfter(new DoAfterEventArgs(user, actualDelay, component.InjectCancel.Token, target)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            MovementThreshold = 0.1f,
            BroadcastFinishedEvent = new BagInjectionCompleteEvent()
            {
                Component = component,
                User = user,
                Target = target,
            },
            BroadcastCancelledEvent = new BagInjectionCancelledEvent()
            {
                Component = component,
            }
        });
    }

    /// <summary>
    /// Begin the continuous flow timer with an initial delay.
    /// </summary>
    private void Connect(IVBagComponent component, EntityUid target, EntityUid user)
    {
        DebugTools.AssertNotNull(target);
        component.Mob = target;
        component.Connected = true;

        component.FlowCancel = new CancellationTokenSource();
        Timer.Spawn(component.FlowStartDelay, () => FlowTimerCallback(component), component.FlowCancel.Token);
    }

    /// <summary>
    /// Kill the flow timer and disconnect from the connected mob (if there is one).
    /// </summary>
    private void Disconnect(IVBagComponent bagComp, bool bPainfully = false)
    {
        // If it's not set to 'Closed' then rip it out.
        if (bPainfully && bagComp.Connected && bagComp.Mob is { Valid: true } mob
            && bagComp.ToggleState != SharedIVBagComponent.IVBagToggleMode.Closed)
        {
            if (TryComp<BloodstreamComponent>(mob, out var bloodstream))
            {
                // Deal just enough blood damage to spill some blood.
                _blood.TryModifyBloodLevel(mob, -(bloodstream.BleedPuddleThreshold + 1f), bloodstream);

                SoundSystem.Play(Filter.Pvs(mob), bloodstream.InstantBloodSound.GetSound(), mob,
                    AudioHelpers.WithVariation(0f).WithVolume(1f).WithMaxDistance(2f));

                _popup.PopupEntity(Loc.GetString("ivbag-component-ripout-text",
                    ("bag", bagComp.Owner)), bagComp.Owner, Filter.Pvs(bagComp.Owner));
            }
        }

        bagComp.Mob = null;
        bagComp.Connected = false;
        bagComp.FlowCancel?.Cancel();
        bagComp.FlowCancel = null;
    }

    /// <summary>
    /// Repetitive IV drip transfer timer.
    /// </summary>
    private void FlowTimerCallback(IVBagComponent bagComp)
    {
        if (bagComp.Deleted) return;

        // Must have a bloodstream and uh.. exist.
        if (bagComp.Mob is not { Valid: true } mob ||
            !TryComp<BloodstreamComponent>(mob, out var bloodstream))
        {
            Disconnect(bagComp);
            return;
        }

        TimeSpan delay;
        switch (bagComp.ToggleState)
        {
            case IVBagComponent.IVBagToggleMode.Inject:
                DripInjectMob(bagComp, bloodstream);
                delay = bagComp.FlowDelay;
                break;
            case IVBagComponent.IVBagToggleMode.Draw:
                DripDrawMob(bagComp, bloodstream);
                // TODO: Drawing is faster on dead things.
                delay = bagComp.FlowDelay;
                break;
            case IVBagComponent.IVBagToggleMode.Closed:
                delay = TimeSpan.FromSeconds(1);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        DebugTools.AssertNotNull(bagComp.FlowCancel);
        Timer.Spawn(bagComp.FlowDelay, () => FlowTimerCallback(bagComp), bagComp.FlowCancel!.Token);
    }

    /// <summary>
    /// Inject both blood and chems into a mob's bloodstream and chemstream respectively using a flexible ratio.
    /// </summary>
    private bool DripInjectMob(IVBagComponent bagComp, BloodstreamComponent bloodstream)
    {
        if (!_solutions.TryGetSolution(bagComp.Owner, IVBagComponent.SolutionName, out var bagSolution))
            return false;

        // Don't bother dripping if there's nothing to drip.
        var bagVolume = bagSolution.TotalVolume;
        var dripAmount = FixedPoint2.Min(bagVolume, bagComp.FlowAmount);
        if (dripAmount <= 0)
            return false;

        // Drip at most this much of either type (chem/blood).
        var dripQuota = FixedPoint2.Min(dripAmount, bagVolume);
        var debugText = "iv injected";

        // Blood should be removed temporarily so that only chems remain.
        var bloodInBag = bagSolution.RemoveReagent(bloodstream.BloodReagent, bagVolume);
        var bloodCanInject = FixedPoint2.Min(dripQuota, bloodInBag, bloodstream.BloodSolution.AvailableVolume);

        // Only chems should remain in the bag now. Blood is returned later.
        var chemInBag = bagSolution.TotalVolume;
        var chemCanInject = FixedPoint2.Min(dripQuota, chemInBag, bloodstream.ChemicalSolution.AvailableVolume);

        // Try to meet the overall quota if either part is lacking.
        var chemQuota = FixedPoint2.Min(dripQuota * bagComp.ChemRatio, chemCanInject, dripQuota);
        var bloodQuota = dripQuota - chemQuota; // Fill the rest with blood.
        if (bloodQuota > bloodCanInject)
        {
            // Blood deficit. Fill the gap with chems.
            bloodQuota = bloodCanInject;
            chemQuota = FixedPoint2.Min(dripQuota - bloodQuota, chemCanInject);
        }

        var bloodToInject = FixedPoint2.Min(bloodQuota, bloodCanInject);
        var chemToInject = FixedPoint2.Min(chemQuota, chemCanInject);

        // Inject the chems.
        if (chemToInject > 0)
        {
            _blood.TryAddToChemicals(bloodstream.Owner,
                _solutions.SplitSolution(bagComp.Owner, bagSolution, chemToInject),
                    bloodstream);

            bloodstream.ChemicalSolution.DoEntityReaction(bloodstream.Owner, ReactionMethod.Injection);

            Console.WriteLine("[IV] injected chems from drip: " + chemToInject);
            debugText += "  [ " + chemToInject + "u chems ]";
        }

        // Inject or at least return the blood we removed.
        if (bloodInBag > 0)
        {
            // Inject the blood.
            if (bloodToInject > 0)
            {
                _blood.TryModifyBloodLevel(bloodstream.Owner, bloodToInject, bloodstream);
                Console.WriteLine("[IV] injected blood from drip: " + bloodToInject);
                debugText += "  [ " + bloodToInject + "u blood ]";
                bloodInBag -= bloodToInject;
            }

            // Make sure all leftover blood returns to the bag.
            _solutions.TryAddReagent(bagComp.Owner, bagSolution, bloodstream.BloodReagent, bloodInBag, out var _);
            Console.WriteLine("[IV] returned blood overflow from drip: " + bloodInBag);
        }

        bool anyInjections = (bloodToInject + chemToInject > 0);
        if (anyInjections)
            _popup.PopupEntity(debugText, bagComp.Owner, Filter.Pvs(bagComp.Owner));

        Dirty(bagComp);
        return anyInjections;
    }

    /// <summary>
    /// Draw both from a target's blood and chemstream, with a ratio determining how much of each.
    /// </summary>
    private bool DripDrawMob(IVBagComponent bagComp, BloodstreamComponent bloodstream)
    {
        if (!_solutions.TryGetSolution(bagComp.Owner, IVBagComponent.SolutionName, out var bagSolution))
            return false;

        var debugText = "iv drawn";

        // Don't bother dripping if we're full.
        var bagCanFill = bagSolution.AvailableVolume;
        var dripQuota = FixedPoint2.Min(bagCanFill, bagComp.FlowAmount);
        if (dripQuota <= 0)
            return false;

        // Try to drain at most a fixed percentage of chems. (for balance)
        var chemSolution = bloodstream.ChemicalSolution;
        var chemDrip = _solutions.SplitSolution(bloodstream.Owner, chemSolution, dripQuota * bagComp.ChemRatio);

        if (chemDrip.TotalVolume > 0 && _solutions.TryAddSolution(bagComp.Owner, bagSolution, chemDrip))
        {
            bagSolution.DoEntityReaction(bagComp.Owner, ReactionMethod.Injection);
            dripQuota -= chemDrip.TotalVolume;
            debugText += "  [ " + chemDrip.TotalVolume + "u chems ]";
        }

        // Drain the rest of the drip quota from their bloodstream.
        // This ensures 100% of the drip will be blood if there were no chems.
        if (dripQuota > 0)
        {
            var bloodReagent = bloodstream.BloodReagent;
            var bloodSolution = bloodstream.BloodSolution;
            var bloodToDraw = FixedPoint2.Min(bloodSolution.TotalVolume, dripQuota);

            if (bloodToDraw > 0)
            {
                _solutions.TryAddReagent(bagComp.Owner, bagSolution, bloodReagent, bloodToDraw, out var _);
                bloodSolution.RemoveReagent(bloodReagent, bloodToDraw);
                debugText += "  [ " + bloodToDraw + "u blood ]";
            }
        }

        _popup.PopupEntity(debugText, bagComp.Owner, Filter.Pvs(bagComp.Owner));

        Dirty(bagComp);
        return true;
    }

    private void TryInject(IVBagComponent component, EntityUid targetEntity, Solution targetSolution, EntityUid user, bool asRefill)
    {
        if (!_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var bagSolution)
            || bagSolution.CurrentVolume == 0)
        {
            return;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(component.PourAmount, targetSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-target-already-full-message",
                ("target", targetEntity)), component.Owner, Filter.Entities(user));
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutions.SplitSolution(component.Owner, bagSolution, realTransferAmount);

        removedSolution.DoEntityReaction(targetEntity, ReactionMethod.Injection);

        if (!asRefill)
        {
            _solutions.Inject(targetEntity, targetSolution, removedSolution);
        }
        else
        {
            _solutions.Refill(targetEntity, targetSolution, removedSolution);
        }

        _popup.PopupEntity(Loc.GetString("injector-component-transfer-success-message",
                ("amount", removedSolution.TotalVolume),
                ("target", targetEntity)), component.Owner, Filter.Entities(user));

        Dirty(component);
        AfterInject(component);
    }

    private void AfterInject(IVBagComponent component)
    {
        // Automatically set syringe to draw after completely draining it.
        if (_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var solution)
            && solution.CurrentVolume == 0)
        {
            component.ToggleState = SharedIVBagComponent.IVBagToggleMode.Draw;
        }
    }

    private void AfterDraw(IVBagComponent component)
    {
        // Automatically set syringe to inject after completely filling it.
        if (_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var solution)
            && solution.AvailableVolume == 0)
        {
            component.ToggleState = SharedIVBagComponent.IVBagToggleMode.Inject;
        }
    }

    private void TryDraw(IVBagComponent component, EntityUid targetEntity, Solution targetSolution, EntityUid user)
    {
        if (!_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var bagSolution)
            || bagSolution.AvailableVolume == 0)
        {
            if (bagSolution != null)
            {
                _popup.PopupEntity(Loc.GetString("injector-component-target-already-full-message",
                    ("target", component.Owner)), component.Owner, Filter.Entities(user));
            }
            return;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(component.PourAmount, targetSolution.DrawAvailable, bagSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-target-is-empty-message", ("target", targetEntity)),
                component.Owner, Filter.Entities(user));
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutions.Draw(targetEntity, targetSolution, realTransferAmount);

        if (!_solutions.TryAddSolution(component.Owner, bagSolution, removedSolution))
        {
            return;
        }

        _popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
                ("amount", removedSolution.TotalVolume),
                ("target", targetEntity)), component.Owner, Filter.Entities(user));

        Dirty(component);
        AfterDraw(component);
    }

    private sealed class BagInjectionCompleteEvent : EntityEventArgs
    {
        public IVBagComponent Component { get; init; } = default!;
        public EntityUid User { get; init; }
        public EntityUid Target { get; init; }
    }

    private sealed class BagInjectionCancelledEvent : EntityEventArgs
    {
        public IVBagComponent Component { get; init; } = default!;
    }
}
