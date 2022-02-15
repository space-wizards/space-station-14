using System.Threading.Tasks;
using System.Threading;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using JetBrains.Annotations;



namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public sealed class MoppingSystem : EntitySystem
{
    //  [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbsorbentComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MoppingDoafterCancel>(OnDoafterCancel);
        SubscribeLocalEvent<MoppingDoafterSuccess>(OnDoafterSuccess);
    }


    private void OnAfterInteract(EntityUid uid, AbsorbentComponent component, AfterInteractEvent args)
    {
        var user = args.User;
        var used = args.Used;
        var clickLocation = args.ClickLocation;

        var solutionSystem = EntitySystem.Get<SolutionContainerSystem>();
        var absorbedSolution = component.AbsorbedSolution;

        var target = args.Target;

        if (absorbedSolution == null)
        {
            return;
        }

        if (!args.CanReach)
        {
            return;
        }


        // if a tile is clicked
        if (args.Target is null
            && !args.Handled)
        {
            // Interact-With-Tile behaviour

            TileRef tile = default!;

            if ((_mapManager.TryGetGrid(clickLocation.GetGridId(EntityManager), out var mapGrid))
                && absorbedSolution != null)
            {
                tile = mapGrid.GetTileRef(clickLocation);

                // Drop some of the absorbed liquid onto the ground
                var solution = solutionSystem.SplitSolution(used, absorbedSolution, FixedPoint2.Min(component.ResidueAmount, component.CurrentVolume));
                EntitySystem.Get<SpillableSystem>().SpillAt(tile, solution, "PuddleSmear");

                args.Handled = true;
            }

            return;
        }

        // if the target has a PuddleComponent
        if (TryComp<PuddleComponent>(args.Target, out var puddle)
            && !args.Handled)
        {
            // Interact-With-Puddle behaviour

            if (!solutionSystem.TryGetSolution(puddle.Owner, puddle.SolutionName, out var puddleSolution)) // if the target has no solution (Note: A solution with zero volume is still a solution!)
            {
                return;
            }

            if (puddleSolution.TotalVolume <= component.MopLowerLimit) // if the puddle is too small for the tool to effectively absorb any more solution from it
            {
                // Dilutes the puddle with whatever is in the tool
                solutionSystem.TryAddSolution(component.Owner, puddleSolution, solutionSystem.SplitSolution(used, absorbedSolution, FixedPoint2.Min(component.ResidueAmount,component.CurrentVolume)));
                return;
            }

            // if the tool is full
            if(component.AvailableVolume <= 0)
            {
                used.PopupMessage(user, Loc.GetString("used-tool-is-full-message"));
                return;
            }

            // Mopping duration (doAfter delay) should scale with PickupAmount (the maximum volume of solution we can pick up with each click).
            var doAfterMoppingArgs = new DoAfterEventArgs(user, component.MopSpeed * component.PickupAmount.Float() / 10.0f,
                target: target)
            {
                BreakOnUserMove = true,
                BreakOnStun = true,
                BreakOnDamage = true,
                MovementThreshold = 0.2f,
                BroadcastCancelledEvent = new MoppingDoafterCancel() { User = user, Tool = used, Target = puddle.Owner, InteractionType = "puddle" },
                BroadcastFinishedEvent = new MoppingDoafterSuccess() { User = user, Tool = used, Target = puddle.Owner, InteractionType = "puddle" }
            };

            // Can't absorb too many entities at once.
            if (component.MaxInteractingEntities < component.InteractingEntities.Count + 1)
                return;

            // Can't mop one puddle multiple times.
            if (!component.InteractingEntities.Add(puddle.Owner))
                return;


            args.Handled = true;
            var mopResult = _doAfterSystem.WaitDoAfter(doAfterMoppingArgs);

                return;
        }

        // For draining the tool into another container.
        if (absorbedSolution is not null && target is not null && component.CurrentVolume > 0 // if tool used has something absorbed
            && !args.Handled)
        {
            // Interact-With-Refillable-Container behaviour:

            if (TryComp<RefillableSolutionComponent>(target, out RefillableSolutionComponent? refillable)) // target has refillable solution component
            {
                // Refilling duration (doAfter delay)
                var refillingDuration = 1.0f; //TODO: Make this scale with how much liquid is in the tool, as well as if the tool needs a wringer for max effect.

                var doAfterRefillingArgs = new DoAfterEventArgs(user, refillingDuration,
                    target: target)
                {
                    BreakOnUserMove = true,
                    BreakOnStun = true,
                    BreakOnDamage = true,
                    MovementThreshold = 0.2f,
                    BroadcastCancelledEvent = new MoppingDoafterCancel() { User = user, Tool = used, Target = refillable.Owner, InteractionType = "refillable" },
                    BroadcastFinishedEvent = new MoppingDoafterSuccess() { User = user, Tool = used, Target = refillable.Owner, InteractionType = "refillable" }
                };

                var refillableResult = _doAfterSystem.WaitDoAfter(doAfterRefillingArgs);
                args.Handled = true;

            }
        }

        // if (absorbedSolution != null)
        //     user.PopupMessage(user, "absorbed solution is null");

        // For wetting the tool from another container. Note that here the absorbedSolution is allowed to be null.
        if (target is not null && component.CurrentVolume <= 0
            && !args.Handled) // if tool used is completely dry
        {
            // Interact-With-Drainable-Container behaviour:

            if (TryComp<DrainableSolutionComponent>(target, out DrainableSolutionComponent? drainableComponent)) // if target has drainable solution component
            {

                                    // Try and get the Solution of the target container, and out var it into "solution."
                                    if (solutionSystem.TryGetSolution(drainableComponent.Owner, drainableComponent.Solution, out var drainableSolution))
                                    {

                                        // Let's transfer up to to half the tool's available capacity to the tool.
                                        var transferAmount = FixedPoint2.Min(0.5*component.AvailableVolume, drainableSolution.CurrentVolume);

                                        if (transferAmount == 0)
                                        {
                                            return;
                                        }

                                        // Remove <transferAmount> units of solution from the target container, and store it in temp var solutionFromContainer.
                                        var solutionFromContainer = solutionSystem.SplitSolution(drainableComponent.Owner, drainableSolution, transferAmount);

                                        // Take that same solutionFromContainer and try adding it to the tool we are refilling.
                                        if (!solutionSystem.TryAddSolution(used, component.AbsorbedSolution, solutionFromContainer))
                                        {
                                            return; //if the attempt fails
                                        }

                                        user.PopupMessage(user, Loc.GetString("bucket-component-mop-is-now-wet-message"));
                    args.Handled = true;
                }
            }
        }



        return;
    }

    private void OnDoafterSuccess(MoppingDoafterSuccess ev)
    {
        if (!TryComp(ev.Tool, out AbsorbentComponent? absorber))
            return;

        if (ev.InteractionType == "puddle")
        {
            if (!TryComp(ev.Target, out PuddleComponent? puddle))
                return;

            var solutionSystem = EntitySystem.Get<SolutionContainerSystem>();

            solutionSystem.TryGetSolution(ev.Tool, "absorbed", out var absorbedSolution); // We will always be looking for a solution named "absorbed" on our AbsorbentComponent.

            if(!solutionSystem.TryGetSolution(ev.Target, puddle.SolutionName, out var puddleSolution))
                return;


            // The volume the mop will take from the puddle
            FixedPoint2 transferAmount;

            // does the puddle actually have reagents? it might not if its a weird cosmetic entity.
            if (puddleSolution.TotalVolume == 0)
                transferAmount = FixedPoint2.Min(absorber.PickupAmount, absorber.AvailableVolume);
            else
            {
                transferAmount = FixedPoint2.Min(absorber.PickupAmount, puddleSolution.TotalVolume, absorber.AvailableVolume);

                if ((puddleSolution.TotalVolume - transferAmount) < absorber.MopLowerLimit) // If the transferAmount would bring the puddle below the MopLowerLimit
                    transferAmount = puddleSolution.TotalVolume - absorber.MopLowerLimit; // Then the transferAmount should bring the puddle down to the MopLowerLimit exactly
            }

            // Transfers solution from the puddle to the mop
            solutionSystem.TryAddSolution(ev.Tool, absorbedSolution, solutionSystem.SplitSolution(ev.Target, puddleSolution, transferAmount));

            SoundSystem.Play(Filter.Pvs(ev.User), absorber.PickupSound.GetSound(), ev.User);

            // if the tool became full after that puddle, let the player know.
            if(absorber.AvailableVolume <= 0)
                ev.User.PopupMessage(ev.User, Loc.GetString("mopping-component-mop-is-now-full-message"));

            absorber.InteractingEntities.Remove(ev.Target); // Tell the absorbentComponent that we have stopped interacting with the target.

        }

        if (ev.InteractionType == "refillable")
        {
            ev.User.PopupMessage(ev.User, "doAfter refillable called.");

            if (!TryComp(ev.Target, out RefillableSolutionComponent? refillable))
                return;

            if (!TryComp(ev.Tool, out AbsorbentComponent? absorbent))
                return;

            var solutionSystem = EntitySystem.Get<SolutionContainerSystem>();

            solutionSystem.TryGetSolution(ev.Tool, "absorbed", out var absorbedSolution); // We will always be looking for a solution named "absorbed" on our AbsorbentComponent.


            // Try and get the Solution of the target container, and out var it into "solution."
            if (solutionSystem.TryGetSolution(refillable.Owner, refillable.Solution, out var solution)
                && absorbedSolution is not null)
            {

                var transferAmount = absorbent.CurrentVolume; // Drain all of the absorbed solution.

                // Remove <transferAmount> units of solution from the used tool, and store it in temp var solutionFromTool.
                var solutionFromTool = solutionSystem.SplitSolution(ev.Tool, absorbedSolution, transferAmount);

                // Take that same solutionFromTool, and try adding it to the container we are refilling.
                if (!solutionSystem.TryAddSolution(refillable.Owner, solution, solutionFromTool))
                {
                    return; //if the attempt fails
                }

                ev.User.PopupMessage(ev.User, Loc.GetString("bucket-component-mop-is-now-dry-message"));
            }

        }



    }

    private void OnDoafterCancel(MoppingDoafterCancel ev)
    {
        if (!TryComp(ev.Tool, out AbsorbentComponent? absorber))
            return;

        absorber.InteractingEntities.Remove(ev.Target); // Tell the absorbentComponent that we have stopped interacting with the target.
        return;
    }





}


public class MoppingDoafterSuccess : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Tool;
    public EntityUid Target;
    public string InteractionType = "none";
}

public class MoppingDoafterCancel : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Tool;
    public EntityUid Target;
    public string InteractionType = "none";
}
