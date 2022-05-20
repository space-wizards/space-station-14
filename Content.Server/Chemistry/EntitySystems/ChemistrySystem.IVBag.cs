using System.Threading;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.CombatMode;
using Content.Server.Disposal.Unit.Components;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Storage.Components;
using Content.Server.UserInterface;
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
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem
{
    public static VerbCategory DelayOptions = new("ivbag-verb-category-drip-rate",
        "/Textures/Interface/VerbIcons/clock.svg.192dpi.png");
    public static VerbCategory FlowOptions = new("ivbag-verb-category-toggle",
        "/Textures/Interface/VerbIcons/refresh.svg.192dpi.png");


    private void InitializeIVBag()
    {
        SubscribeLocalEvent<IVBagComponent, ComponentStartup>(OnBagStartup);
        SubscribeLocalEvent<IVBagComponent, ComponentGetState>(OnBagGetState);
        SubscribeLocalEvent<IVBagComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<IVBagComponent, MoveEvent>(HandleBagMove);

        SubscribeLocalEvent<IVBagComponent, HandDeselectedEvent>(OnBagDeselected);
        SubscribeLocalEvent<IVBagComponent, UseInHandEvent>(OnBagUse);
        SubscribeLocalEvent<IVBagComponent, AfterInteractEvent>(OnBagAfterInteract);
        SubscribeLocalEvent<IVBagComponent, ActivateInWorldEvent>(OnWorldActivate);
        SubscribeLocalEvent<IVBagComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        // SubscribeLocalEvent<IVBagComponent, DragDropEvent>(HandleDragDropOn); // TODO: stand interactions

        SubscribeLocalEvent<IVBagComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<IVBagComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<IVBagComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

        SubscribeLocalEvent<BagInjectionCompleteEvent>(OnBagInjectionComplete);
        SubscribeLocalEvent<BagInjectionCancelledEvent>(OnBagInjectionCancelled);
    }


    #region IV Bag Verbs

    /// <summary>
    ///     Add an alt-click interaction for disconnection.
    /// </summary>
    private void OnGetAltVerbs(EntityUid uid, IVBagComponent bagComp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!bagComp.Connected || !args.CanInteract || !args.CanAccess)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("ivbag-verb-disconnect"),
            Act = () => Disconnect(bagComp, false, args.User),
            IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png",
            Priority = 911
        });
    }

    /// <summary>
    ///     Add manual interactions for setting drip rate and flow valve.
    /// </summary>
    private void OnGetVerbs(EntityUid uid, IVBagComponent bagComp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        // Flow Valve Settings
        foreach (var mode in Enum.GetValues<IVBagComponent.IVBagToggleMode>())
        {
            var isSelected = (mode == bagComp.FlowState);
            var name = IVBagComponent.FlowStateName(mode);

            var verb = new Verb()
            {
                Category = FlowOptions,
                Text = Loc.GetString(isSelected ?
                    "ivbag-verb-flow-set-current" : "ivbag-verb-flow-set",
                    ("name", name)),
                Disabled = isSelected,
                Priority = -100 * ((int) mode)
            };

            verb.Act = isSelected ? null : () =>
            {
                SetFlowState(bagComp, args.User, mode);
            };

            args.Verbs.Add(verb);
        }

        // Delay Settings
        if (IVBagComponent.FlowDelayOptions.Length < 2)
            return;

        foreach (var option in IVBagComponent.FlowDelayOptions)
        {
            var isSelected = MathHelper.CloseTo(option, bagComp.FlowDelay.Seconds);

            var verb = new Verb()
            {
                Category = DelayOptions,
                Text = Loc.GetString(isSelected ?
                    "ivbag-verb-delay-set-current" : "ivbag-verb-delay-set",
                    ("time", option)),
                Disabled = isSelected,
                Priority = (int) (-100 * option)
            };

            verb.Act = isSelected ? null : () =>
            {
                bagComp.FlowDelay = TimeSpan.FromSeconds(option);
                _popup.PopupEntity(Loc.GetString("popup-trigger-timer-set",
                    ("time", option)), args.User, Filter.Entities(args.User));
            };

            args.Verbs.Add(verb);
        }
    }

    #endregion



    private void OnInsertAttempt(EntityUid uid, IVBagComponent bagComp, ContainerGettingInsertedAttemptEvent args)
    {
        if (bagComp.Connected)
            return;

        // Don't allow people to drip IVs from their backpack.
        // We don't need powergamers with 20u healchem per second tanking everything.
        if (HasComp<ServerStorageComponent>(args.Container.Owner))
        {
            // Console.WriteLine("[IV] tried to insert into storage container");
            Disconnect(bagComp, true);
        }
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
        string modeName = IVBagComponent.FlowStateName(bagComp.FlowState);

        args.PushMarkup(Loc.GetString("ivbag-state-examine",
            ("mode", modeName)));

        if (bagComp.Connected && bagComp.Target is { Valid: true } mob)
            args.PushMarkup(Loc.GetString("ivbag-connected-examine",
                ("color", Color.White.ToHexNoAlpha()),
                ("target", mob)));
    }

    private void OnBagUse(EntityUid uid, IVBagComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        Toggle(component, args.User);
        args.Handled = true;
    }

    private static void OnBagDeselected(EntityUid uid, IVBagComponent component, HandDeselectedEvent args)
    {
        component.InjectCancel?.Cancel();
        component.InjectCancel = null;
    }

    /// <summary>
    ///     Rip out the bag if it moves too far while connected.
    /// </summary>
    private void HandleBagMove(EntityUid uid, IVBagComponent bagComp, ref MoveEvent args)
    {
        if (!bagComp.Connected || bagComp.TargetPos == null)
            return;

        args.NewPosition.TryDistance(EntityManager, bagComp.TargetPos.Coordinates, out var dist);
        Console.WriteLine("distance: " + dist);

        if (!args.NewPosition.InRange(EntityManager, bagComp.TargetPos.Coordinates, IVBagComponent.BreakDistance))
        {
            Disconnect(bagComp, true); // tomfromtheshowtomandjerryscreaming.ogg
        }
    }

    private void OnBagStartup(EntityUid uid, IVBagComponent component, ComponentStartup args)
    {
        Dirty(component);
    }

    private void OnSolutionChange(EntityUid uid, IVBagComponent component, SolutionChangedEvent args)
    {
        Dirty(component);
    }

    private void OnBagGetState(EntityUid uid, IVBagComponent component, ref ComponentGetState args)
    {
        _solutions.TryGetSolution(uid, IVBagComponent.SolutionName, out var solution);

        var currentVolume = solution?.CurrentVolume ?? FixedPoint2.Zero;
        var maxVolume = solution?.MaxVolume ?? FixedPoint2.Zero;

        args.State = new SharedIVBagComponent.IVBagComponentState(currentVolume, maxVolume, component.FlowState, component.Connected);
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

    /// <summary>
    ///     Set the inject/draw/closed state and provide a popup.
    /// </summary>
    private void SetFlowState(IVBagComponent bagComp, EntityUid user, IVBagComponent.IVBagToggleMode newState)
    {
        if (bagComp.FlowState == newState)
            return;

        bagComp.FlowState = newState;

        string msg = newState switch
        {
            IVBagComponent.IVBagToggleMode.Draw => Loc.GetString("ivbag-component-drawing-text"),
            IVBagComponent.IVBagToggleMode.Inject => Loc.GetString("ivbag-component-injecting-text"),
            IVBagComponent.IVBagToggleMode.Closed => Loc.GetString("ivbag-component-closed-text"),
            _ => throw new ArgumentOutOfRangeException()
            // _ => Loc.GetString("injector-invalid-injector-toggle-mode")
        };

        _popup.PopupEntity(Loc.GetString(msg), bagComp.Owner, Filter.Entities(user));

        // Reset the flow timer after toggling.
        if (bagComp.Target is { Valid: true } mob)
            SetFlowTimer(bagComp, bagComp.FlowStartDelay, cancelPrevious: true);
    }

    /// <summary>
    ///     Toggle between inject/draw/closed states.
    /// </summary>
    private void Toggle(IVBagComponent bagComp, EntityUid user)
    {
        SetFlowState(bagComp, user,
            bagComp.FlowState switch
            {
                IVBagComponent.IVBagToggleMode.Inject => IVBagComponent.IVBagToggleMode.Draw,
                IVBagComponent.IVBagToggleMode.Draw => IVBagComponent.IVBagToggleMode.Closed,
                IVBagComponent.IVBagToggleMode.Closed => IVBagComponent.IVBagToggleMode.Inject,
                _ => throw new ArgumentOutOfRangeException()
            }
        );
    }

    private void OnBagAfterInteract(EntityUid uid, IVBagComponent bagComp, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach ||
            args.Target is not { Valid: true } target)
            return;

        if (bagComp.InjectCancel != null)
        {
            args.Handled = true;
            return;
        }


        if (bagComp.Connected)
        {
            if (target == bagComp.Target)
            {
                // Quickly remove the needle upon double-activate.
                Disconnect(bagComp, ripOut: false);
                return;
            }
            else if (HasComp<DisposalUnitComponent>(target))
            {
                // Disconnect if put in a trash bin. Otherwise it takes a long time to disconnect.
                Disconnect(bagComp, ripOut: true);
                return;
            }
        }

        // Is the target a mob? If yes, use a do-after to give them time to respond.
        if (HasComp<MobStateComponent>(target) ||
            HasComp<BloodstreamComponent>(target))
        {
            Disconnect(bagComp, true); // switching target
            InjectDoAfter(bagComp, args.User, target);
            args.Handled = true;
            return;
        }
        else if (HasComp<SolutionContainerManagerComponent>(uid))
        {
            // Don't override default behaviors if injection failed.
            args.Handled = UseInjector(target, args.User, bagComp);
            return;
        }
    }

    private bool UseInjector(EntityUid target, EntityUid user, IVBagComponent bagComp)
    {
        if (HasComp<BloodstreamComponent>(target))
        {
            Connect(bagComp, target, user);
            return true;
        }

        if (bagComp.FlowState == IVBagComponent.IVBagToggleMode.Inject)
        {
            if (_solutions.TryGetRefillableSolution(target, out var refillableSolution))
            {
                TryInject(bagComp, target, refillableSolution, user, true);
            }
            else if (_solutions.TryGetInjectableSolution(target, out var injectableSolution))
            {
                TryInject(bagComp, target, injectableSolution, user, false);
            }
            else
            {
                return false;
            }
        }
        else if (bagComp.FlowState == IVBagComponent.IVBagToggleMode.Draw)
        {
            if (_solutions.TryGetDrawableSolution(target, out var drawableSolution))
            {
                TryDraw(bagComp, target, drawableSolution, user);
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Send informative pop-up messages and wait for a do-after to complete.
    /// </summary>
    private void InjectDoAfter(IVBagComponent component, EntityUid user, EntityUid target)
    {
        // Create a pop-up for the user
        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, Filter.Entities(user));

        if (!_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var solution))
            return;

        // Halt any existing flows.
        if (component.Connected)
            Disconnect(component, true);

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
            if (component.FlowState == SharedIVBagComponent.IVBagToggleMode.Inject)
            {
                _logs.Add(LogType.ForceFeed,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to inject {EntityManager.ToPrettyString(target):target} with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}");
            }
        }
        else
        {
            // Self-injections take half as long.
            actualDelay /= 2;

            if (component.FlowState == SharedIVBagComponent.IVBagToggleMode.Inject)
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
    ///     Begin the continuous flow timer with an initial delay.
    /// </summary>
    private void Connect(IVBagComponent bagComp, EntityUid target, EntityUid user)
    {
        DebugTools.AssertNotNull(target);

        bagComp.Target = target;
        bagComp.TargetPos = EntityManager.GetComponent<TransformComponent>(target);
        bagComp.Connected = true;

        SetFlowTimer(bagComp, bagComp.FlowStartDelay, cancelPrevious: true);
    }

    /// <summary>
    ///     Kill the flow timer and disconnect from the connected mob (if there is one).
    /// </summary>
    private void Disconnect(IVBagComponent bagComp, bool ripOut = false, EntityUid? remoteUser = null)
    {
        if (bagComp.Connected)
        {
            if (bagComp.Target is { Valid: true } mob)
            {
                if (ripOut && bagComp.FlowState != SharedIVBagComponent.IVBagToggleMode.Closed
                    && TryComp<BloodstreamComponent>(mob, out var bloodstream))
                {
                    // Deal just enough blood damage to spill some blood.
                    _blood.TryModifyBloodLevel(mob, -(bloodstream.BleedPuddleThreshold + 1f), bloodstream);

                    SoundSystem.Play(Filter.Pvs(mob), bloodstream.InstantBloodSound.GetSound(), mob,
                        AudioHelpers.WithVariation(0f).WithVolume(1f).WithMaxDistance(2f));

                    _popup.PopupEntity(Loc.GetString("ivbag-component-ripout-text",
                        ("bag", bagComp.Owner)), mob, Filter.Pvs(mob));
                }
                else
                {
                    // Safely pull out.
                    EntityUid[] sendTo = (remoteUser is { Valid: true } user)
                        ? new EntityUid[] { mob, user }
                        : new EntityUid[] { mob };

                    _popup.PopupEntity(Loc.GetString("ivbag-component-remove-text",
                        ("bag", bagComp.Owner)), mob, Filter.Entities(sendTo));
                }
            }

            bagComp.Target = null;
            bagComp.TargetPos = null;
            bagComp.Connected = false;
        }

        // Let the bag spill continuously if ripped out while in inject mode.
        if (bagComp.FlowState != SharedIVBagComponent.IVBagToggleMode.Inject)
            StopFlowTimer(bagComp);
    }

    /// <summary>
    ///     Set an existing timer, or create a new timer and set its initial delay.
    /// </summary>
    private void SetFlowTimer(IVBagComponent bagComp, TimeSpan initialDelay, bool cancelPrevious = true)
    {
        if (cancelPrevious)
            StopFlowTimer(bagComp);

        if (bagComp.FlowCancel == null)
            bagComp.FlowCancel = new CancellationTokenSource();

        Timer.Spawn(initialDelay, () => FlowTimerCallback(bagComp), bagComp.FlowCancel.Token);
    }

    /// <summary>
    ///     Force the dripping to stop.
    /// </summary>
    private void StopFlowTimer(IVBagComponent bagComp)
    {
        if (bagComp.FlowCancel == null)
            return;

        bagComp.FlowCancel?.Cancel();
        bagComp.FlowCancel = null;
    }

    /// <summary>
    ///     Self-repeating IV drip transfer timer.
    /// </summary>
    private void FlowTimerCallback(IVBagComponent bagComp)
    {
        if (bagComp.Deleted || bagComp.FlowCancel == null) return;

        // Must have a bloodstream and uh.. exist.
        if (bagComp.Target is { Valid: true } mob
            && TryComp<BloodstreamComponent>(mob, out var bloodstream))
        {
            TimeSpan delay;
            switch (bagComp.FlowState)
            {
                case IVBagComponent.IVBagToggleMode.Inject:
                    FlowIntoMob(bagComp, bloodstream);
                    delay = bagComp.FlowDelay;
                    break;
                case IVBagComponent.IVBagToggleMode.Draw:
                    FlowFromMob(bagComp, bloodstream);
                    delay = bagComp.FlowDelay;
                    break;
                case IVBagComponent.IVBagToggleMode.Closed:
                    delay = TimeSpan.FromSeconds(1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        /*else if (bagComp.ToggleState == SharedIVBagComponent.IVBagToggleMode.Inject
            && _solutions.TryGetSolution(bagComp.Owner, IVBagComponent.SolutionName,
            out var bagSolution))
        {
            if (bagComp.Connected)
                Disconnect(bagComp);

            // Stop spilling once empty.
            var spill = _solutions.SplitSolution(bagComp.Owner, bagSolution, bagComp.FlowAmount);
            if (spill.CurrentVolume == 0)
                return;

            // Continue to spew our contents if left open.
            // TODO: Have spill puddle update its color and match any reagent.
            _spillableSystem.SpillAt(bagComp.Owner, spill, "PuddleSplatter", false, true);
        }*/
        else
        {
            Disconnect(bagComp);
        }

        SetFlowTimer(bagComp, bagComp.FlowDelay, cancelPrevious: false);
    }

    /// <summary>
    ///     Inject both blood and chems into a mob's bloodstream, with a limit on chems.
    /// </summary>
    private bool FlowIntoMob(IVBagComponent bagComp, BloodstreamComponent bloodstream)
    {
        if (!_solutions.TryGetSolution(bagComp.Owner, IVBagComponent.SolutionName, out var bagSolution))
            return false;

        // var debugText = "iv injected";

        // Drip at most this much of either type (chem/blood).
        var bagVolume = bagSolution.TotalVolume;
        var dripQuota = FixedPoint2.Min(bagComp.FlowAmount, bagVolume);

        // Don't bother dripping if there's nothing to drip.
        if (dripQuota <= 0)
            return false;

        // Blood should be removed temporarily so that only chems remain.
        var bloodInBag = bagSolution.RemoveReagent(bloodstream.BloodReagent, bagVolume);
        var bloodCanInject = FixedPoint2.Min(dripQuota, bloodInBag, bloodstream.BloodSolution.AvailableVolume);

        // Only chems should remain in the bag now. Blood is returned later.
        var chemInBag = bagSolution.TotalVolume;
        var chemCanInject = FixedPoint2.Min(dripQuota, chemInBag, bloodstream.ChemicalSolution.AvailableVolume);

        // Limit chem flow and fill the rest of the drip with any available blood.
        var chemToInject = FixedPoint2.Min(chemCanInject, dripQuota, bagComp.FlowChem);
        var bloodToInject = FixedPoint2.Min(bloodCanInject, dripQuota - chemToInject); // Fill the rest with blood.

        // Inject the chems.
        if (chemToInject > 0)
        {
            _blood.TryAddToChemicals(bloodstream.Owner,
                _solutions.SplitSolution(bagComp.Owner, bagSolution, chemToInject),
                bloodstream);

            bloodstream.ChemicalSolution.DoEntityReaction(bloodstream.Owner, ReactionMethod.Injection);

            // Console.WriteLine("[IV] injected chems from drip: " + chemToInject);
            // debugText += "  [ " + chemToInject + "u chems ]";
        }

        // Inject or at least return the blood we removed.
        if (bloodInBag > 0)
        {
            // Inject the blood.
            if (bloodToInject > 0)
            {
                _blood.TryModifyBloodLevel(bloodstream.Owner, bloodToInject, bloodstream);
                // Console.WriteLine("[IV] injected blood from drip: " + bloodToInject);
                // debugText += "  [ " + bloodToInject + "u blood ]";
                bloodInBag -= bloodToInject;
            }

            // Make sure all leftover blood returns to the bag.
            _solutions.TryAddReagent(bagComp.Owner, bagSolution, bloodstream.BloodReagent, bloodInBag, out var _);
            // Console.WriteLine("[IV] returned blood overflow from drip: " + bloodInBag);
        }

        bool anyDrips = (bloodToInject + chemToInject > 0);
        if (anyDrips)
        {
            // _popup.PopupEntity(debugText, bagComp.Owner, Filter.Pvs(bagComp.Owner));
            _popup.PopupEntity(Loc.GetString("ivbag-component-drip-text"),
                bagComp.Owner, Filter.Entities(bloodstream.Owner));
        }

        Dirty(bagComp);
        return anyDrips;
    }

    /// <summary>
    ///     Draw both from a target's blood and chemstream, with a limit on chems.
    /// </summary>
    private bool FlowFromMob(IVBagComponent bagComp, BloodstreamComponent bloodstream)
    {
        if (!_solutions.TryGetSolution(bagComp.Owner, IVBagComponent.SolutionName, out var bagSolution))
            return false;

        // Don't bother dripping if we're full.
        var dripQuota = FixedPoint2.Min(bagSolution.AvailableVolume, bagComp.FlowAmount);
        if (dripQuota <= 0)
            return false;

        // Try to drain a limited amount of chems per drip.
        // var debugText = "iv drawn";
        var anyDrips = false;
        var chemSolution = bloodstream.ChemicalSolution;
        var chemDrip = _solutions.SplitSolution(bloodstream.Owner, chemSolution, bagComp.FlowChem);

        if (_solutions.TryAddSolution(bagComp.Owner, bagSolution, chemDrip))
        {
            anyDrips = true;
            bagSolution.DoEntityReaction(bagComp.Owner, ReactionMethod.Injection);
            dripQuota -= chemDrip.TotalVolume;
            // debugText += "  [ " + chemDrip.TotalVolume + "u chems ]";
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
                anyDrips = true;
                _solutions.TryAddReagent(bagComp.Owner, bagSolution, bloodReagent, bloodToDraw, out var _);
                bloodSolution.RemoveReagent(bloodReagent, bloodToDraw);
                // debugText += "  [ " + bloodToDraw + "u blood ]";
            }
        }

        if (anyDrips)
        {
            // _popup.PopupEntity(debugText, bagComp.Owner, Filter.Pvs(bagComp.Owner));
            _popup.PopupEntity(Loc.GetString("ivbag-component-drip-text"),
                bagComp.Owner, Filter.Entities(bloodstream.Owner));
        }

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
        var toInject = FixedPoint2.Min(component.PourAmount, targetSolution.AvailableVolume);

        if (toInject <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-target-already-full-message",
                ("target", targetEntity)), component.Owner, Filter.Entities(user));
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutions.SplitSolution(component.Owner, bagSolution, toInject);

        removedSolution.DoEntityReaction(targetEntity, ReactionMethod.Injection);

        if (asRefill)
            _solutions.Refill(targetEntity, targetSolution, removedSolution);
        else
            _solutions.Inject(targetEntity, targetSolution, removedSolution);

        _popup.PopupEntity(Loc.GetString("injector-component-transfer-success-message",
                ("amount", removedSolution.TotalVolume),
                ("target", targetEntity)), component.Owner, Filter.Entities(user));

        Dirty(component);
        AfterInject(component);
    }

    private void AfterInject(IVBagComponent bagComp)
    {
        // Rip out if trying to interact with solutions while connected.
        if (bagComp.Connected)
            Disconnect(bagComp, true);

        // Automatically close the syringe after completely draining it.
        if (_solutions.TryGetSolution(bagComp.Owner, IVBagComponent.SolutionName, out var solution)
            && solution.CurrentVolume == 0)
        {
            bagComp.FlowState = SharedIVBagComponent.IVBagToggleMode.Closed;
        }
    }

    private void AfterDraw(IVBagComponent bagComp)
    {
        // Rip out if trying to interact with solutions while connected.
        if (bagComp.Connected)
            Disconnect(bagComp, true);

        // Automatically set syringe to inject after completely filling it.
        if (_solutions.TryGetSolution(bagComp.Owner, IVBagComponent.SolutionName, out var solution)
            && solution.AvailableVolume == 0)
        {
            bagComp.FlowState = SharedIVBagComponent.IVBagToggleMode.Inject;
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
