using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Map;
using JetBrains.Annotations;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public sealed class MoppingSystem : EntitySystem
{
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private SolutionContainerSystem _solutionSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbsorbentComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<TransferCancelledEvent>(OnTransferCancelled);
        SubscribeLocalEvent<TransferCompleteEvent>(OnTransferComplete);
    }

    private void ReleaseToFloor(EntityCoordinates clickLocation, AbsorbentComponent absorbent, Solution? absorbedSolution)
    {

        if ((_mapManager.TryGetGrid(clickLocation.GetGridId(EntityManager), out var mapGrid)) // needs valid grid
            && absorbedSolution != null) // needs a solution to place on the tile
        {
            TileRef tile = mapGrid.GetTileRef(clickLocation);

            // Drop some of the absorbed liquid onto the ground
            var releaseAmount = FixedPoint2.Min(absorbent.ResidueAmount, absorbedSolution.CurrentVolume);
            var releasedSolution = _solutionSystem.SplitSolution(absorbent.Owner, absorbedSolution, releaseAmount);
            EntitySystem.Get<SpillableSystem>().SpillAt(tile, releasedSolution, "PuddleSmear");
        }
        return;
    }

    private void TryTransfer(EntityUid donor, EntityUid acceptor, string solutionName)
    {
        //_solutionSystem.TryGetSolution()

    }

    private void TransferDoAfter(EntityUid user, EntityUid used, EntityUid target, AbsorbentComponent component, FixedPoint2 currentVolume, FixedPoint2 availableVolume)
    {
    // If we get this far, it means a do_after bar is almost guaranteed to start.

        var delay = 1.0f; //default do_after delay in seconds.
        var msg = "";
        var targetSolutionName = "";
        var transferAmount = FixedPoint2.New(0);

        if (TryComp<PuddleComponent>(target, out var puddle))
        {
            // These return conditions will abort BEFORE the do_after is called:
            if (availableVolume < 0) // mop is completely full
            {
                msg = "mopping-system-tool-full";
                user.PopupMessage(user, Loc.GetString(msg)); // play message now because we are returning.
                return;
            }

            // Determine transferAmount
            transferAmount = ;

            delay = (component.PickupAmount.Float() / 10.0f) * component.MopSpeed;
            targetSolutionName = puddle.SolutionName;

        }
        else if (currentVolume > 0) // mop is wet
        {
            if (TryComp<RefillableSolutionComponent>(target, out var refillable))
            {
                // These return conditions will abort BEFORE the do_after is called:
                if (!_solutionSystem.TryGetRefillableSolution(target, out var refillableSolution))
                    return;
                else if (refillableSolution.AvailableVolume <= 0) // destination container is full
                {
                    msg = "mopping-system-target-container-full";
                    user.PopupMessage(user, Loc.GetString(msg)); // play message now because we are returning.
                    return;
                }

                // Determine transferAmount
                transferAmount = ;

                // set delay/popup/sound if nondefault
                delay = 1.0f; //TODO: Make this scale with how much liquid is in the tool, as well as if the tool needs a wringer for max effect.
                targetSolutionName = refillable.Solution;
                msg = "mopping-system-refillable-message";
            }
        }
        else if (currentVolume <= 0) // mop is dry
        {
            if (TryComp<DrainableSolutionComponent>(target, out var drainable))
            {
                // These return conditions will abort BEFORE the do_after is called:
                if (!_solutionSystem.TryGetDrainableSolution(target, out var drainableSolution))
                    return;
                else if (drainableSolution.CurrentVolume <= 0) // source container is empty
                {
                    msg = "mopping-system-target-container-empty";
                    user.PopupMessage(user, Loc.GetString(msg)); // play message now because we are returning.
                    return;
                }

                // Determine transferAmount
                transferAmount = ;

                // set delay/popup/sound if nondefault
                targetSolutionName = drainable.Solution;
                msg = "mopping-system-drainable-message";
            }
        }
        else return;

        var doAfterArgs = new DoAfterEventArgs(user, delay, target: target)
        {
            BreakOnUserMove = true,
            BreakOnStun = true,
            BreakOnDamage = true,
            MovementThreshold = 0.2f,
            BroadcastCancelledEvent = new TransferCancelledEvent()
            {
                Component = component, // (the AbsorbentComponent)
                Target = target
            },
            BroadcastFinishedEvent = new TransferCompleteEvent()
            {
                User = user,
                Tool = used,
                Target = target,
                TargetSolutionName = targetSolutionName,
                Message = msg,
                TransferAmount = transferAmount
            }
        };

        // Can't interact with too many entities at once.
        if (component.MaxInteractingEntities < component.InteractingEntities.Count + 1)
            return;

        // Can't interact with the same container multiple times at once.
        if (!component.InteractingEntities.Add(target))
            return;

        var result = _doAfterSystem.WaitDoAfter(doAfterArgs);

    }


    private void OnAfterInteract(EntityUid uid, AbsorbentComponent component, AfterInteractEvent args)
    {
        var user = args.User;
        var used = args.Used;
        var target = args.Target;

        var solutionSystem = EntitySystem.Get<SolutionContainerSystem>();
        solutionSystem.TryGetSolution(used, "absorbed", out var absorbedSolution);

        var toolAvailableVolume = FixedPoint2.New(0);
        var toolCurrentVolume = FixedPoint2.New(0);
        var transferAmount = FixedPoint2.New(0);

        if (absorbedSolution is not null)
        {
            toolAvailableVolume = absorbedSolution.AvailableVolume;
            toolCurrentVolume = absorbedSolution.CurrentVolume;
        }

        if (!args.CanReach)
        {
            return;
        }

        // For adding liquid to an empty floor tile
        if (args.Target is null // if a tile is clicked
            && !args.Handled)
        {
            ReleaseToFloor(args.ClickLocation, component, absorbedSolution);
            args.Handled = true;
            return;
        }


        // For mopping up or adding to puddles
        if (TryComp<PuddleComponent>(args.Target, out var puddle)
            && !args.Handled)
        {
            if (!solutionSystem.TryGetSolution(puddle.Owner, puddle.SolutionName, out var puddleSolution)) // if the target has no solution
            {
                return;
            }

            // adding to puddles
            if (puddleSolution.TotalVolume <= component.MopLowerLimit // if the puddle is too small for the tool to effectively absorb any more solution from it
                && absorbedSolution is not null) // tool needs a solution to dilute the puddle with.
            {
                // Dilutes the puddle with whatever is in the tool
                solutionSystem.TryAddSolution(component.Owner, puddleSolution, solutionSystem.SplitSolution(used, absorbedSolution, FixedPoint2.Min(component.ResidueAmount, toolCurrentVolume)));
                args.Handled = true;
                return;
            }

            // Everything else is handled in this call.
            if (target is not null)
            {
                TransferDoAfter(user, used, target.Value, component, toolCurrentVolume, toolAvailableVolume);
            }
            args.Handled = true;
        }


        // For draining the tool into another container.
        if (target is not null && toolCurrentVolume > 0 // if tool used has something absorbed
            && !args.Handled)
        {
            if (TryComp<RefillableSolutionComponent>(target, out RefillableSolutionComponent? refillable)) // target has refillable solution component
            {
                solutionSystem.TryGetSolution(refillable.Owner, refillable.Solution, out Solution? refillableSolution);


                if (target is not null)
                {
                    TransferDoAfter(user, used, target.Value, component, toolCurrentVolume, toolAvailableVolume);
                }
                args.Handled = true;
            }
        }


        // For wetting the tool from another container.
        if (target is not null && toolCurrentVolume <= 0 // if tool used is completely dry
            && !args.Handled)
        {
            if (TryComp<DrainableSolutionComponent>(target, out DrainableSolutionComponent? drainable)) // if target has drainable solution component
            {
                solutionSystem.TryGetSolution(drainable.Owner, drainable.Solution, out Solution? drainableSolution);

                if (drainableSolution is null // if the target drainable is empty
                    || toolAvailableVolume <= 0) // or if the tool is full
                {
                    return;
                }

                if (target is not null)
                {
                    TransferDoAfter(user, used, target.Value, component, toolCurrentVolume, toolAvailableVolume);
                }
                args.Handled = true;
            }
        }
        return;
    }

    private void OnTransferComplete(TransferCompleteEvent ev)
    {

        // Play the After SFX
        // Play the After popup message










    //     if (!TryComp(ev.Tool, out AbsorbentComponent? absorbent))
    //         return;

    //     var solutionSystem = EntitySystem.Get<SolutionContainerSystem>();

    //     solutionSystem.TryGetSolution(ev.Tool, "absorbed", out var absorbedSolution); // We will always be looking for a solution named "absorbed" on our AbsorbentComponent.

    //     var toolAvailableVolume = FixedPoint2.New(0);
    //     var toolCurrentVolume = FixedPoint2.New(0);
    //     var transferAmount = FixedPoint2.New(0);

    //     if (absorbedSolution is not null)
    //     {
    //         toolAvailableVolume = absorbedSolution.AvailableVolume;
    //         toolCurrentVolume = absorbedSolution.CurrentVolume;
    //     }








    //     // Interact-With-Puddle behaviour:
    //     if (ev.InteractionType == "puddle")
    //     {
    //         // if (!TryComp(ev.Target, out PuddleComponent? puddle))
    //         // {
    //         //     absorbent.InteractingEntities.Remove(ev.Target);
    //         //     return;
    //         // }

    //         if(!solutionSystem.TryGetSolution(ev.Target, puddle.SolutionName, out var puddleSolution))
    //         {
    //             absorbent.InteractingEntities.Remove(ev.Target);
    //             return;
    //         }

    //         // does the puddle actually have reagents? it might not if its a weird cosmetic entity.
    //         if (puddleSolution.TotalVolume == 0)
    //             transferAmount = FixedPoint2.Min(absorbent.PickupAmount, toolAvailableVolume);
    //         else
    //         {
    //             transferAmount = FixedPoint2.Min(absorbent.PickupAmount, puddleSolution.TotalVolume, toolAvailableVolume);

    //             if ((puddleSolution.TotalVolume - transferAmount) < absorbent.MopLowerLimit) // If the transferAmount would bring the puddle below the MopLowerLimit
    //                 transferAmount = puddleSolution.TotalVolume - absorbent.MopLowerLimit; // Then the transferAmount should bring the puddle down to the MopLowerLimit exactly
    //         }

    //         // Transfers solution from the puddle to the mop
    //         solutionSystem.TryAddSolution(ev.Tool, absorbedSolution, solutionSystem.SplitSolution(ev.Target, puddleSolution, transferAmount));

    //         SoundSystem.Play(Filter.Pvs(ev.User), absorbent.PickupSound.GetSound(), ev.User);

    //         // if the tool became full after that puddle, let the player know.
    //         if(toolAvailableVolume <= 0)
    //             ev.User.PopupMessage(ev.User, Loc.GetString("mopping-component-mop-is-now-full-message"));
    //     }


    //     // Interact-With-Refillable-Container behaviour:
    //     if (ev.InteractionType == "refillable")
    //     {
    //         if (!TryComp(ev.Target, out RefillableSolutionComponent? refillable))
    //         {
    //             absorbent.InteractingEntities.Remove(ev.Target);
    //             return;
    //         }

    //         // Try and get the Solution of the target container, and out var it into "solution."
    //         if (solutionSystem.TryGetSolution(refillable.Owner, refillable.Solution, out var solution)
    //             && absorbedSolution is not null) // Tool needs a solution to transfer.
    //         {
    //             transferAmount = toolCurrentVolume; // Drain all of the absorbed solution.

    //             // Remove <transferAmount> units of solution from the used tool, and store it in temp var solutionFromTool.
    //             var solutionFromTool = solutionSystem.SplitSolution(ev.Tool, absorbedSolution, transferAmount);

    //             // Take that same solutionFromTool, and try adding it to the container we are refilling.
    //             if (!solutionSystem.TryAddSolution(refillable.Owner, solution, solutionFromTool))
    //             {
    //                 absorbent.InteractingEntities.Remove(ev.Target);
    //                 return; //if the attempt fails
    //             }

    //             SoundSystem.Play(Filter.Pvs(ev.User), absorbent.TransferSound.GetSound(), ev.User);
    //             ev.User.PopupMessage(ev.User, Loc.GetString("bucket-component-mop-is-now-dry-message"));
    //         }
    //     }


    //     // Interact-With-Drainable-Container behaviour:
    //     if (ev.InteractionType == "drainable")
    //     {
    //         if (!TryComp(ev.Target, out DrainableSolutionComponent? drainable))
    //         {
    //             absorbent.InteractingEntities.Remove(ev.Target);
    //             return;
    //         }

    //         // Try and get the Solution of the target container, and out var it into "solution."
    //         if (solutionSystem.TryGetSolution(drainable.Owner, drainable.Solution, out var drainableSolution))
    //         {

    //             // Let's transfer up to to half the tool's available capacity to the tool.
    //             transferAmount = FixedPoint2.Min(0.5*toolAvailableVolume, drainableSolution.CurrentVolume);

    //             if (transferAmount == 0)
    //             {
    //                 absorbent.InteractingEntities.Remove(ev.Target);
    //                 return;
    //             }

    //             // Remove <transferAmount> units of solution from the target container, and store it in temp var solutionFromContainer.
    //             var solutionFromContainer = solutionSystem.SplitSolution(drainable.Owner, drainableSolution, transferAmount);

    //             // Take that same solutionFromContainer and try adding it to the tool we are refilling.
    //             if (!solutionSystem.TryAddSolution(ev.Tool, absorbedSolution, solutionFromContainer))
    //             {
    //                 absorbent.InteractingEntities.Remove(ev.Target);
    //                 return; //if the attempt fails
    //             }

    //             SoundSystem.Play(Filter.Pvs(ev.User), absorbent.TransferSound.GetSound(), ev.User);
    //             ev.User.PopupMessage(ev.User, Loc.GetString("bucket-component-mop-is-now-wet-message"));
    //         }
    //     }

    //     absorbent.InteractingEntities.Remove(ev.Target); // Tell the absorbentComponent that we have stopped interacting with the target.
    }

    private void OnTransferCancelled(TransferCancelledEvent ev)
    {
        ev.Component.InteractingEntities.Remove(ev.Target); // Tell the absorbentComponent that we have stopped interacting with the target.
        return;
    }

}


public sealed class TransferCompleteEvent : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Tool;
    public EntityUid Target;
    public string TargetSolutionName = "";
    public string Message = "";
    public FixedPoint2 TransferAmount;

}

public sealed class TransferCancelledEvent : EntityEventArgs
{
    public AbsorbentComponent Component { get; init; } = default!;
    public EntityUid Target;

}
