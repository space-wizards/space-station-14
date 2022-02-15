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

        if (!args.Handled)
        {
            // if a tile is clicked
            if (args.Target is null)
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
            if (TryComp<PuddleComponent>(args.Target, out var puddle))
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
                var doAfterArgs = new DoAfterEventArgs(user, component.MopSpeed * component.PickupAmount.Float() / 10.0f,
                    target: target)
                {
                    BreakOnUserMove = true,
                    BreakOnStun = true,
                    BreakOnDamage = true,
                    MovementThreshold = 0.2f,
                    BroadcastCancelledEvent = new MoppingDoafterCancel() { User = user, Tool = used, Target = puddle.Owner },
                    BroadcastFinishedEvent = new MoppingDoafterSuccess() { User = user, Tool = used, Target = puddle.Owner }
                };

                // Can't absorb too many entities at once.
                if (component.MaxAbsorbingEntities < component.AbsorbingEntities.Count + 1)
                    return;

                // Can't mop one puddle multiple times.
                if (!component.AbsorbingEntities.Add(puddle.Owner))
                    return;


                args.Handled = true;
                var result = _doAfterSystem.WaitDoAfter(doAfterArgs);

                    return;
            }

            // For draining the tool into another container.
            if (absorbedSolution is not null && target is not null && component.CurrentVolume > 0) // if tool used has something absorbed
            {
                // Interact-With-Refillable-Container behaviour:

                if (TryComp<RefillableSolutionComponent>(target, out RefillableSolutionComponent? refillableComponent)) // target has refillable solution component
                {
                    // Try and get the Solution of the target container, and out var it into "solution."
                    if (solutionSystem.TryGetSolution(refillableComponent.Owner, refillableComponent.Solution, out var solution))
                    {

                        var transferAmount = component.CurrentVolume; // Drain all of the absorbed solution.

                        // Remove <transferAmount> units of solution from the used tool, and store it in temp var solutionFromTool.
                        var solutionFromTool = solutionSystem.SplitSolution(used, absorbedSolution, transferAmount);

                        // Take that same solutionFromTool, and try adding it to the container we are refilling.
                        if (!solutionSystem.TryAddSolution(refillableComponent.Owner, solution, solutionFromTool))
                        {
                            return; //if the attempt fails
                        }
                    }

                    user.PopupMessage(user, Loc.GetString("bucket-component-mop-is-now-dry-message"));
                    args.Handled = true;
                }
            }

            // if (absorbedSolution != null)
            //     user.PopupMessage(user, "absorbed solution is null");

            // For wetting the tool from another container. Note that here the absorbedSolution is allowed to be null.
            if (target is not null && component.CurrentVolume <= 0) // if tool used is completely dry
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
                        solutionSystem.TryAddSolution(used, component.AbsorbedSolution, solutionFromContainer);

                        // if (!solutionSystem.TryAddSolution(used, component.AbsorbedSolution, solutionFromContainer))
                        // {
                        //     user.PopupMessage(user, "Attempt failed");
                        //     return; //if the attempt fails
                        // }
                    }

                    user.PopupMessage(user, Loc.GetString("bucket-component-mop-is-now-wet-message"));
                    args.Handled = true;
                }
            }

        }

        return;
    }

    // private void OnPuddleInteractUsing(EntityUid puddleUid, PuddleComponent component, InteractUsingEvent args)
    // {
    //     /*
    //     * Functionality:
    //     * "tool" refers to the mop, sponge, rag, etc. being used in the interaction.
    //     * If an empty tile is clicked
    //     *       Spill some of the tool's contents there, if any.
    //     * Else, if a puddle is clicked, and if the tool can mop it up,
    //     *       Try to mop some of it up using the tool.
    //     * Else, transfer some of the tool's contents to the puddle to dilute it.
    //     *
    //     * Depending on the tool used, it may not be able to pick up the last little bit of the puddle (see:MoppingLowerLimit)
    //     * In this case, diluting it can replace most of the contaminant with water, which will evaporate.
    //     * The original contaminant may also be able to evaporate on its own.
    //     */

    //     if (!TryComp<AbsorbentComponent>(args.Used, out var absorber)) // if the tool used does not have an absorbent component
    //         return;

    //     var solutionSystem = EntitySystem.Get<SolutionContainerSystem>();

    //     var user = args.User;
    //     var used = args.Used;
    //     var target = args.Target;

    //      //const string absorbedSolutionName = "absorbed"; // We will always be looking for a solution with this name on our AbsorbentComponent.

    //     if (!solutionSystem.TryGetSolution(used, "absorbed", out var absorbedSolution) // If the tool used doesn't have a solution container
    //         || absorber.CurrentlyAbsorbing)                                            // or if it's busy
    //     {
    //         return;
    //     }

    //     if (!solutionSystem.TryGetSolution(component.Owner, component.SolutionName, out var puddleSolution)) // if the target has no solution (Note: A solution with zero volume is still a solution!)
    //     {
    //         return;
    //     }

    //     if (puddleSolution.TotalVolume <= absorber.MopLowerLimit) // if the puddle is too small for the tool to effectively absorb any more solution from it
    //     {
    //         // Add solution from the tool to the puddle (the opposite of absorption)
    //         solutionSystem.TryAddSolution(component.Owner, puddleSolution, solutionSystem.SplitSolution(used, absorbedSolution, FixedPoint2.Min(absorber.ResidueAmount,absorber.CurrentVolume)));
    //         return;
    //     }

    //     // if the mop is full
    //     if(absorber.AvailableVolume <= 0)
    //     {
    //         used.PopupMessage(user, Loc.GetString("used-tool-is-full-message"));
    //         return;
    //     }

    //     // Mopping duration (aka delay) should scale with PickupAmount and not puddle volume, because we are picking up a constant volume of solution with each click.
    //     var doAfterArgs = new DoAfterEventArgs(user, absorber.MopSpeed * absorber.PickupAmount.Float() / 10.0f,
    //         target: target)
    //     {
    //         BreakOnUserMove = true,
    //         BreakOnStun = true,
    //         BreakOnDamage = true,
    //         MovementThreshold = 0.2f,
    //         BroadcastCancelledEvent = new MoppingDoafterCancel() { User = user, Tool = used, Target = component.Owner },
    //         BroadcastFinishedEvent = new MoppingDoafterSuccess() { User = user, Tool = used, Target = component.Owner }
    //     };

    //     // Can't absorb too many entities at once.
    //     if (absorber.MaxAbsorbingEntities < absorber.AbsorbingEntities.Count + 1)
    //         return;

    //     // Can't mop one puddle multiple times.
    //     if (!absorber.AbsorbingEntities.Add(puddleUid))
    //         return;


    //     var result = EntitySystem.Get<DoAfterSystem>().WaitDoAfter(doAfterArgs);


    // }

    // private void OnAbsorbentUsing(EntityUid absorbentUid, AbsorbentComponent component, InteractUsingEvent args)
    // {
    //     if (!TryComp<AbsorbentComponent>(args.Used, out var absorber)) // if the tool used does not have an absorbent component
    //         return;

    //     var solutionSystem = EntitySystem.Get<SolutionContainerSystem>();

    //     var user = args.User;
    //     var used = args.Used;
    //     var target = args.Target;

    // }


    // // private void OnDrainableInteractUsing(EntityUid drainableUid, DrainableSolutionComponent component, InteractUsingEvent args)
    // // {
    // //     if (!TryComp<AbsorbentComponent>(args.Used, out var absorber)) // if the tool used does not have an absorbent component
    // //         return;

    // //     var solutionSystem = EntitySystem.Get<SolutionContainerSystem>();

    // //     var user = args.User;
    // //     var used = args.Used;
    // //     var target = args.Target;

    // // }


    private void OnDoafterSuccess(MoppingDoafterSuccess ev)
    {
        if (!TryComp(ev.Tool, out AbsorbentComponent? absorber))
            return;

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

        absorber.AbsorbingEntities.Remove(ev.Target); // Tell the absorbentComponent that we have stopped interacting with the target.

    }

    private void OnDoafterCancel(MoppingDoafterCancel ev)
    {
        if (!TryComp(ev.Tool, out AbsorbentComponent? absorber))
            return;

        absorber.AbsorbingEntities.Remove(ev.Target); // Tell the absorbentComponent that we have stopped interacting with the target.
        return;
    }





}


public class MoppingDoafterSuccess : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Tool;
    public EntityUid Target;
}

public class MoppingDoafterCancel : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Tool;
    public EntityUid Target;
}
