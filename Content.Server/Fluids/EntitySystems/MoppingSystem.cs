using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Map;
using JetBrains.Annotations;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public sealed class MoppingSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SpillableSystem _spillableSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;

    const string puddlePrototypeId = "PuddleSmear"; // The puddle prototype to use when releasing liquid to the floor, making a new puddle

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbsorbentComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<TransferCancelledEvent>(OnTransferCancelled);
        SubscribeLocalEvent<TransferCompleteEvent>(OnTransferComplete);
    }

    private void OnAfterInteract(EntityUid uid, AbsorbentComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach) // if user cannot reach the target
        {
            return;
        }

        if (args.Handled) // if the event was already handled
        {
            return;
        }

        _solutionSystem.TryGetSolution(args.Used, AbsorbentComponent.SolutionName, out var absorbedSolution);

        if (absorbedSolution is null)
        {
            return;
        }

        var toolAvailableVolume = absorbedSolution.AvailableVolume;
        var toolCurrentVolume = absorbedSolution.CurrentVolume;

        // For adding liquid to an empty floor tile
        if (args.Target is null) // if a tile is clicked
        {
            ReleaseToFloor(args.ClickLocation, component, absorbedSolution);
            args.Handled = true;
            args.User.PopupMessage(args.User, Loc.GetString("mopping-system-release-to-floor"));
            return;
        }
        else if (args.Target is not null)
        {
            // Handle our do_after logic
            HandleDoAfter(args.User, args.Used, args.Target.Value, component, toolCurrentVolume, toolAvailableVolume);
        }

        args.Handled = true;
        return;
    }

    private void ReleaseToFloor(EntityCoordinates clickLocation, AbsorbentComponent absorbent, Solution? absorbedSolution)
    {
        if ((_mapManager.TryGetGrid(clickLocation.GetGridUid(EntityManager), out var mapGrid)) // needs valid grid
            && absorbedSolution is not null) // needs a solution to place on the tile
        {
            TileRef tile = mapGrid.GetTileRef(clickLocation);

            // Drop some of the absorbed liquid onto the ground
            var releaseAmount = FixedPoint2.Min(absorbent.ResidueAmount, absorbedSolution.CurrentVolume); // The release amount specified on the absorbent component, or the amount currently absorbed (whichever is less).
            var releasedSolution = _solutionSystem.SplitSolution(absorbent.Owner, absorbedSolution, releaseAmount); // Remove releaseAmount of solution from the absorbent component
            _spillableSystem.SpillAt(tile, releasedSolution, puddlePrototypeId);                                    // And spill it onto the tile.
        }
    }

    // Handles logic for our different types of valid target.
    // Checks for conditions that would prevent a doAfter from starting.
    private void HandleDoAfter(EntityUid user, EntityUid used, EntityUid target, AbsorbentComponent component, FixedPoint2 currentVolume, FixedPoint2 availableVolume)
    {
        // Below variables will be set within this function depending on what kind of target was clicked.
        // They will be passed to the OnTransferComplete if the doAfter succeeds.

        EntityUid donor;
        EntityUid acceptor;
        string donorSolutionName;
        string acceptorSolutionName;

        FixedPoint2 transferAmount;

        var delay = 1.0f; //default do_after delay in seconds.
        string msg;
        SoundSpecifier sfx;

        // For our purposes, if our target has a PuddleComponent, treat it as a puddle above all else.
        if (TryComp<PuddleComponent>(target, out var puddle))
        {
            // These return conditions will abort BEFORE the do_after is called:
            if(!_solutionSystem.TryGetSolution(target, puddle.SolutionName, out var puddleSolution) // puddle Solution is null
                || (puddleSolution.TotalVolume <= 0)) // puddle is completely empty
            {
                return;
            }
            else if (availableVolume < 0) // mop is completely full
            {
                msg = "mopping-system-tool-full";
                user.PopupMessage(user, Loc.GetString(msg, ("used", used))); // play message now because we are aborting.
                return;
            }
            // adding to puddles
            else if (puddleSolution.TotalVolume < component.MopLowerLimit // if the puddle is too small for the tool to effectively absorb any more solution from it
                    && currentVolume > 0) // tool needs a solution to dilute the puddle with.
            {
                // Dilutes the puddle with some solution from the tool
                transferAmount = FixedPoint2.Max(component.ResidueAmount, currentVolume);
                TryTransfer(used, target, "absorbed", puddle.SolutionName, transferAmount); // Complete the transfer right away, with no doAfter.

                sfx = component.TransferSound;
                SoundSystem.Play(sfx.GetSound(), Filter.Pvs(user), used); // Give instant feedback for diluting puddle, so that it's clear that the player is adding to the puddle (as opposed to other behaviours, which have a doAfter).

                msg = "mopping-system-puddle-diluted";
                user.PopupMessage(user, Loc.GetString(msg)); // play message now because we are aborting.

                return; // Do not begin a doAfter.
            }
            else
            {
                // Taking from puddles:

                // Determine transferAmount:
                transferAmount = FixedPoint2.Min(component.PickupAmount, puddleSolution.TotalVolume, availableVolume);

                // TODO: consider onelining this with the above, using additional args on Min()?
                if ((puddleSolution.TotalVolume - transferAmount) < component.MopLowerLimit) // If the transferAmount would bring the puddle below the MopLowerLimit
                {
                    transferAmount = puddleSolution.TotalVolume - component.MopLowerLimit; // Then the transferAmount should bring the puddle down to the MopLowerLimit exactly
                }

                donor = target; // the puddle Uid
                donorSolutionName = puddle.SolutionName;

                acceptor  = used; // the mop/tool Uid
                acceptorSolutionName = "absorbed"; // by definition on AbsorbentComponent

                // Set delay/popup/sound if nondefault. Popup and sound will only play on a successful doAfter.
                delay = (component.PickupAmount.Float() / 10.0f) * component.MopSpeed; // Delay should scale with PickupAmount, which represents the maximum we can pick up per click.
                msg = "mopping-system-puddle-success";
                sfx = component.PickupSound;

                DoMopInteraction(user, used, target, donor, acceptor, component, donorSolutionName, acceptorSolutionName, transferAmount, delay, msg, sfx);
            }
        }
        else if ((TryComp<RefillableSolutionComponent>(target, out var refillable)) // We can put solution from the tool into the target
                && (currentVolume > 0))                                             // And the tool is wet
        {
            // These return conditions will abort BEFORE the do_after is called:
            if (!_solutionSystem.TryGetRefillableSolution(target, out var refillableSolution)) // refillable Solution is null
            {
                return;
            }
            else if (refillableSolution.AvailableVolume <= 0) // target container is full (liquid destination)
            {
                msg = "mopping-system-target-container-full";
                user.PopupMessage(user, Loc.GetString(msg, ("target", target))); // play message now because we are aborting.
                return;
            }
            else if (refillableSolution.MaxVolume <= FixedPoint2.New(20)) // target container is too small (e.g. syringe)
            {
                msg = "mopping-system-target-container-too-small";
                user.PopupMessage(user, Loc.GetString(msg, ("target", target))); // play message now because we are aborting.
                return;
            }
            else
            {
                // Determine transferAmount
                if (_tagSystem.HasTag(used, "Mop") // if the tool used is a literal mop (and not a sponge, rag, etc.)
                    && !_tagSystem.HasTag(target, "Wringer")) // and if the target does not have a wringer for properly drying the mop
                {
                    delay = 5.0f; // Should take much longer if you don't have a wringer

                    if ((currentVolume / (currentVolume + availableVolume) ) > 0.25) // mop is more than one-quarter full
                    {
                        transferAmount = FixedPoint2.Min(refillableSolution.AvailableVolume, currentVolume * 0.6); // squeeze up to 60% of the solution from the mop.
                        msg = "mopping-system-hand-squeeze-little-wet";

                        if ((currentVolume / (currentVolume + availableVolume) ) > 0.5) // if the mop is more than half full
                            msg = "mopping-system-hand-squeeze-still-wet"; // overwrites the above

                    }
                    else // mop is less than one-quarter full
                    {
                        transferAmount = FixedPoint2.Min(refillableSolution.AvailableVolume, currentVolume); // squeeze remainder of solution from the mop.
                        msg = "mopping-system-hand-squeeze-dry";
                    }

                }
                else
                {
                    transferAmount = FixedPoint2.Min(refillableSolution.AvailableVolume, currentVolume); //Transfer all liquid from the tool to the container, but only if it will fit.
                    msg = "mopping-system-refillable-success";
                    delay = 1.0f;
                }

                donor = used; // the mop/tool Uid
                donorSolutionName = "absorbed"; // by definition on AbsorbentComponent

                acceptor  = target; // the refillable container's Uid
                acceptorSolutionName = refillable.Solution;

                // Set delay/popup/sound if nondefault. Popup and sound will only play on a successful doAfter.

                sfx = component.TransferSound;

                DoMopInteraction(user, used, target, donor, acceptor, component, donorSolutionName, acceptorSolutionName, transferAmount, delay, msg, sfx);
            }
        }
        else if (TryComp<DrainableSolutionComponent>(target, out var drainable) // We can take solution from the target
                && currentVolume <= 0 ) // tool is dry
        {
            // These return conditions will abort BEFORE the do_after is called:
            if (!_solutionSystem.TryGetDrainableSolution(target, out var drainableSolution))
            {
                return;
            }
            else if (drainableSolution.CurrentVolume <= 0) // target container is empty (liquid source)
            {
                msg = "mopping-system-target-container-empty";
                user.PopupMessage(user, Loc.GetString(msg, ("target", target))); // play message now because we are returning.
                return;
            }
            else
            {
                // Determine transferAmount
                transferAmount = FixedPoint2.Min(availableVolume * 0.5, drainableSolution.CurrentVolume); // Let's transfer up to to half the tool's available capacity to the tool.

                donor = target; // the drainable container's Uid
                donorSolutionName = drainable.Solution;

                acceptor  = used; // the mop/tool Uid
                acceptorSolutionName = "absorbed"; // by definition on AbsorbentComponent

                // Set delay/popup/sound if nondefault. Popup and sound will only play on a successful doAfter.
                // default delay is fine for this case.
                msg = "mopping-system-drainable-success";
                sfx = component.TransferSound;

                DoMopInteraction(user, used, target, donor, acceptor, component, donorSolutionName, acceptorSolutionName, transferAmount, delay, msg, sfx);
            }
        }
    }

    private void DoMopInteraction(EntityUid user, EntityUid used, EntityUid target, EntityUid donor, EntityUid acceptor,
                                  AbsorbentComponent component, string donorSolutionName, string acceptorSolutionName,
                                  FixedPoint2 transferAmount, float delay, string msg, SoundSpecifier sfx)
    {
        var doAfterArgs = new DoAfterEventArgs(user, delay, target: target)
        {
            BreakOnUserMove = true,
            BreakOnStun = true,
            BreakOnDamage = true,
            MovementThreshold = 0.2f,
            BroadcastCancelledEvent = new TransferCancelledEvent()
            {
                Target = target,
                Component = component // (the AbsorbentComponent)
            },
            BroadcastFinishedEvent = new TransferCompleteEvent()
            {
                User = user,
                Tool = used,
                Target = target,
                Donor = donor,
                Acceptor = acceptor,
                Component = component,
                DonorSolutionName = donorSolutionName,
                AcceptorSolutionName = acceptorSolutionName,
                Message = msg,
                Sound = sfx,
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

    private void OnTransferComplete(TransferCompleteEvent ev)
    {
        SoundSystem.Play(ev.Sound.GetSound(), Filter.Pvs(ev.User), ev.Tool); // Play the After SFX

        ev.User.PopupMessage(ev.User, Loc.GetString(ev.Message, ("target", ev.Target), ("used", ev.Tool))); // Play the After popup message

        TryTransfer(ev.Donor, ev.Acceptor, ev.DonorSolutionName, ev.AcceptorSolutionName, ev.TransferAmount);

        ev.Component.InteractingEntities.Remove(ev.Target); // Tell the absorbentComponent that we have stopped interacting with the target.
        return;
    }

    private void OnTransferCancelled(TransferCancelledEvent ev)
    {
        if (!ev.Component.Deleted)
            ev.Component.InteractingEntities.Remove(ev.Target); // Tell the absorbentComponent that we have stopped interacting with the target.

        return;
    }

    private void TryTransfer(EntityUid donor, EntityUid acceptor, string donorSolutionName, string acceptorSolutionName, FixedPoint2 transferAmount)
    {
        if (_solutionSystem.TryGetSolution(donor, donorSolutionName, out var donorSolution) // If the donor solution is valid
            && _solutionSystem.TryGetSolution(acceptor, acceptorSolutionName, out var acceptorSolution)) // And the acceptor solution is valid
        {
            var solutionToTransfer = _solutionSystem.SplitSolution(donor, donorSolution, transferAmount);   // Split a portion of the donor solution
            _solutionSystem.TryAddSolution(acceptor, acceptorSolution, solutionToTransfer);                 // And add it to the acceptor solution
        }
    }
}


public sealed class TransferCompleteEvent : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Tool;
    public EntityUid Target;
    public EntityUid Donor;
    public EntityUid Acceptor;
    public AbsorbentComponent Component { get; init; } = default!;
    public string DonorSolutionName = "";
    public string AcceptorSolutionName = "";
    public string Message = "";
    public SoundSpecifier Sound { get; init; } = default!;
    public FixedPoint2 TransferAmount;

}

public sealed class TransferCancelledEvent : EntityEventArgs
{
    public EntityUid Target;
    public AbsorbentComponent Component { get; init; } = default!;

}
