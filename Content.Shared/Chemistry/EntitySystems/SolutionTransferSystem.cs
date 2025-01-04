using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Shared.Plankton;
using System.Collections.Generic;
using Robust.Shared.Random;
using System.Linq;


namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Allows an entity to transfer solutions with a customizable amount per click.
/// Also provides <see cref="Transfer"/> API for other systems.
/// </summary>
public sealed class SolutionTransferSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    ///     Default transfer amounts for the set-transfer verb.
    /// </summary>
    public static readonly FixedPoint2[] DefaultTransferAmounts = new FixedPoint2[] { 1, 5, 10, 25, 50, 100, 250, 500, 1000 };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionTransferComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
        SubscribeLocalEvent<SolutionTransferComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SolutionTransferComponent, TransferAmountSetValueMessage>(OnTransferAmountSetValueMessage);
    }

    private void OnTransferAmountSetValueMessage(Entity<SolutionTransferComponent> ent, ref TransferAmountSetValueMessage message)
    {
        var (uid, comp) = ent;

        var newTransferAmount = FixedPoint2.Clamp(message.Value, comp.MinimumTransferAmount, comp.MaximumTransferAmount);
        comp.TransferAmount = newTransferAmount;

        if (message.Actor is { Valid: true } user)
            _popup.PopupEntity(Loc.GetString("comp-solution-transfer-set-amount", ("amount", newTransferAmount)), uid, user);

        Dirty(uid, comp);
    }

    private void AddSetTransferVerbs(Entity<SolutionTransferComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var (uid, comp) = ent;

        if (!args.CanAccess || !args.CanInteract || !comp.CanChangeTransferAmount || args.Hands == null)
            return;

        // Custom transfer verb
        var @event = args;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("comp-solution-transfer-verb-custom-amount"),
            Category = VerbCategory.SetTransferAmount,
            // TODO: remove server check when bui prediction is a thing
            Act = () =>
            {
                _ui.OpenUi(uid, TransferAmountUiKey.Key, @event.User);
            },
            Priority = 1
        });

        // Add specific transfer verbs according to the container's size
        var priority = 0;
        var user = args.User;
        foreach (var amount in DefaultTransferAmounts)
        {
            AlternativeVerb verb = new();
            verb.Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount));
            verb.Category = VerbCategory.SetTransferAmount;
            verb.Act = () =>
            {
                comp.TransferAmount = amount;

                _popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), uid, user);

                Dirty(uid, comp);
            };

            // we want to sort by size, not alphabetically by the verb text.
            verb.Priority = priority;
            priority--;

            args.Verbs.Add(verb);
        }
    }

    private void OnAfterInteract(Entity<SolutionTransferComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not {} target)
            return;

        var (uid, comp) = ent;

        //Special case for reagent tanks, because normally clicking another container will give solution, not take it.
        if (comp.CanReceive
            && !HasComp<RefillableSolutionComponent>(target) // target must not be refillable (e.g. Reagent Tanks)
            && _solution.TryGetDrainableSolution(target, out var targetSoln, out _) // target must be drainable
            && TryComp<RefillableSolutionComponent>(uid, out var refill)
            && _solution.TryGetRefillableSolution((uid, refill, null), out var ownerSoln, out var ownerRefill))
        {
            var transferAmount = comp.TransferAmount; // This is the player-configurable transfer amount of "uid," not the target reagent tank.

            // if the receiver has a smaller transfer limit, use that instead
            if (refill?.MaxRefill is {} maxRefill)
                transferAmount = FixedPoint2.Min(transferAmount, maxRefill);

            var transferred = Transfer(args.User, target, targetSoln.Value, uid, ownerSoln.Value, transferAmount);
            args.Handled = true;
            if (transferred > 0)
            {
                var toTheBrim = ownerRefill.AvailableVolume == 0;
                var msg = toTheBrim
                    ? "comp-solution-transfer-fill-fully"
                    : "comp-solution-transfer-fill-normal";

                _popup.PopupClient(Loc.GetString(msg, ("owner", args.Target), ("amount", transferred), ("target", uid)), uid, args.User);
                return;
            }
        }

        // if target is refillable, and owner is drainable
        if (comp.CanSend
            && TryComp<RefillableSolutionComponent>(target, out var targetRefill)
            && _solution.TryGetRefillableSolution((target, targetRefill, null), out targetSoln, out _)
            && _solution.TryGetDrainableSolution(uid, out ownerSoln, out _))
        {
            var transferAmount = comp.TransferAmount;

            if (targetRefill?.MaxRefill is {} maxRefill)
                transferAmount = FixedPoint2.Min(transferAmount, maxRefill);

            var transferred = Transfer(args.User, uid, ownerSoln.Value, target, targetSoln.Value, transferAmount);
            args.Handled = true;
            if (transferred > 0)
            {
                var message = Loc.GetString("comp-solution-transfer-transfer-solution", ("amount", transferred), ("target", target));
                _popup.PopupClient(message, uid, args.User);
            }
        }
    }

    /// <summary>
    /// Transfer from a solution to another, allowing either entity to cancel it and show a popup.
    /// </summary>
    /// <returns>The actual amount transferred.</returns>
public FixedPoint2 Transfer(EntityUid user,
    EntityUid sourceEntity,
    Entity<SolutionComponent> source,
    EntityUid targetEntity,
    Entity<SolutionComponent> target,
    FixedPoint2 amount)
{
    var transferAttempt = new SolutionTransferAttemptEvent(sourceEntity, targetEntity);

    // Check if the source is cancelling the transfer
    RaiseLocalEvent(sourceEntity, ref transferAttempt);
    if (transferAttempt.CancelReason is {} reason)
    {
        _popup.PopupClient(reason, sourceEntity, user);
        return FixedPoint2.Zero;
    }

    var sourceSolution = source.Comp.Solution;
    if (sourceSolution.Volume == 0)
    {
        _popup.PopupClient(Loc.GetString("comp-solution-transfer-is-empty", ("target", sourceEntity)), sourceEntity, user);
        return FixedPoint2.Zero;
    }

    // Check if the target is cancelling the transfer
    RaiseLocalEvent(targetEntity, ref transferAttempt);
    if (transferAttempt.CancelReason is {} targetReason)
    {
        _popup.PopupClient(targetReason, targetEntity, user);
        return FixedPoint2.Zero;
    }

    var targetSolution = target.Comp.Solution;
    if (targetSolution.AvailableVolume == 0)
    {
        _popup.PopupClient(Loc.GetString("comp-solution-transfer-is-full", ("target", targetEntity)), targetEntity, user);
        return FixedPoint2.Zero;
    }

    // Calculate the actual amount to transfer
    var actualAmount = FixedPoint2.Min(amount, FixedPoint2.Min(sourceSolution.Volume, targetSolution.AvailableVolume));

    // Handle plankton species transfer from source to target
    if (TryComp<PlanktonComponent>(sourceEntity, out var planktonSource))
    {
        var planktonFraction = actualAmount / sourceSolution.Volume;

        // Handle plankton transfer to the target if it has a PlanktonComponent
        if (TryComp<PlanktonComponent>(targetEntity, out var planktonTarget))
        {
            // Transfer plankton species only if they are not already in the target container
            foreach (var species in planktonSource.SpeciesInstances)
            {
                // Check if the species already exists in the target container
                var existingSpecies = planktonTarget.SpeciesInstances
                    .FirstOrDefault(s => s.SpeciesName.ToString() == species.SpeciesName.ToString());

                if (existingSpecies != null)
                {
                    // If the species exists in target, update the current size safely
                    existingSpecies.CurrentSize += species.CurrentSize * (float)planktonFraction;
                }
                else
                {
                    // If it doesn't exist, add a new instance of this species to the target container
                    planktonTarget.SpeciesInstances.Add(new PlanktonComponent.PlanktonSpeciesInstance(
                        species.SpeciesName,
                        species.Diet,
                        species.Characteristics,
                        species.CurrentSize * (float)planktonFraction, // Apply fraction to new species added
                        species.CurrentHunger,
                        species.IsAlive
                    ));
                }
            }
        }
        else
        {
            // If the target doesn't have a PlanktonComponent, create a new one and add species
            planktonTarget = new PlanktonComponent();
            foreach (var species in planktonSource.SpeciesInstances)
            {
                planktonTarget.SpeciesInstances.Add(new PlanktonComponent.PlanktonSpeciesInstance(
                    species.SpeciesName,
                    species.Diet,
                    species.Characteristics,
                    species.CurrentSize * (float)planktonFraction, // Apply fraction to new species added
                    species.CurrentHunger,
                    species.IsAlive
                ));
            }
            _entityManager.AddComponent(targetEntity, planktonTarget);
        }

        // Safely update plankton species sizes in the source (reduce based on the fraction)
        foreach (var species in planktonSource.SpeciesInstances.ToList()) // Use ToList() to avoid modifying collection during iteration
        {
            species.CurrentSize -= species.CurrentSize * (float)planktonFraction;
            if (species.CurrentSize < 0)
                species.CurrentSize = 0;
        }
    }

    // Handle solution transfer logic
    var solution = _solution.SplitSolution(source, actualAmount);
    _solution.AddSolution(target, solution);

    TransferPlanktonComponent(sourceEntity, targetEntity);

    var ev = new SolutionTransferredEvent(sourceEntity, targetEntity, user, actualAmount);
    RaiseLocalEvent(targetEntity, ref ev);

    _adminLogger.Add(LogType.Action, LogImpact.Medium,
        $"{ToPrettyString(user):player} transferred {SharedSolutionContainerSystem.ToPrettyString(solution)} to {ToPrettyString(targetEntity):target}, which now contains {SharedSolutionContainerSystem.ToPrettyString(targetSolution)}");

    if (sourceSolution.Volume == 0) // if the container being poured is empty, remove the planktoncomponent.
    {
        if (HasComp<PlanktonComponent>(sourceEntity))
        {
            _entityManager.RemoveComponent<PlanktonComponent>(sourceEntity);
        }
    }

    return actualAmount;
}






private void TransferPlanktonComponent(EntityUid sourceEntity, EntityUid targetEntity)
{
    if (TryComp<PlanktonComponent>(sourceEntity, out var planktonSource))
    {
        // If the target doesn't have a PlanktonComponent, add one
        if (!HasComp<PlanktonComponent>(targetEntity))
        {
            _entityManager.AddComponent<PlanktonComponent>(targetEntity);
            Log.Info($"Added PlanktonComponent to {targetEntity}");
        }

        var planktonTarget = Comp<PlanktonComponent>(targetEntity);

        // Transfer other plankton-related properties (ReagentId, Diet, Characteristics, etc.)
        planktonTarget.ReagentId = planktonSource.ReagentId;
        planktonTarget.DeadPlankton = planktonSource.DeadPlankton;
        planktonTarget.Diet = planktonSource.Diet;
        planktonTarget.Characteristics = planktonSource.Characteristics;
        planktonTarget.TemperatureToleranceLow = planktonSource.TemperatureToleranceLow;
        planktonTarget.TemperatureToleranceHigh = planktonSource.TemperatureToleranceHigh;

        // If there's a separator component, transfer only one species
        if (TryComp<PlanktonSeparatorComponent>(sourceEntity, out _))
        {
            // Randomly select a species to transfer
            if (planktonSource.SpeciesInstances.Count > 0)
            {
                var randomIndex = _random.Next(planktonSource.SpeciesInstances.Count);
                var selectedSpecies = planktonSource.SpeciesInstances[randomIndex];

                // Check if the species already exists in the target container
                var existingSpecies = planktonTarget.SpeciesInstances
                    .FirstOrDefault(s => s.SpeciesName.ToString() == selectedSpecies.SpeciesName.ToString());

                if (existingSpecies != null)
                {
                    // If the species exists, just add to the current size
                    existingSpecies.CurrentSize += selectedSpecies.CurrentSize;
                    if (existingSpecies.CurrentSize < 0)
                    {
                        existingSpecies.CurrentSize = 0; // Prevent negative size
                    }
                }
                else
                {
                    // Add a new species if it doesn't exist in the target
                    planktonTarget.SpeciesInstances.Add(new PlanktonComponent.PlanktonSpeciesInstance(
                        selectedSpecies.SpeciesName,
                        selectedSpecies.Diet,
                        selectedSpecies.Characteristics,
                        selectedSpecies.CurrentSize,
                        selectedSpecies.CurrentHunger,
                        selectedSpecies.IsAlive
                    ));
                }

                // Log for debugging
                Log.Info($"Plankton separator transferred species: {selectedSpecies.SpeciesName}");
            }
        }
        else
        {
            // If no separator, transfer all species normally (but avoid duplicates)
            foreach (var species in planktonSource.SpeciesInstances)
            {
                // Check if the species already exists in the target container
                var existingSpecies = planktonTarget.SpeciesInstances
                    .FirstOrDefault(s => s.SpeciesName.ToString() == species.SpeciesName.ToString());

                if (existingSpecies != null)
                {
                    // Update size if species exists
                    existingSpecies.CurrentSize += species.CurrentSize;
                    if (existingSpecies.CurrentSize < 0)
                    {
                        existingSpecies.CurrentSize = 0;
                    }
                }
                else
                {
                    // Add the species to the target if it doesn't exist
                    planktonTarget.SpeciesInstances.Add(new PlanktonComponent.PlanktonSpeciesInstance(
                        species.SpeciesName,
                        species.Diet,
                        species.Characteristics,
                        species.CurrentSize,
                        species.CurrentHunger,
                        species.IsAlive
                    ));
                }
            }
        }
    }
}




}

/// <summary>
/// Raised when attempting to transfer from one solution to another.
/// Raised on both the source and target entities so either can cancel the transfer.
/// To not mispredict this should always be cancelled in shared code and not server or client.
/// </summary>
[ByRefEvent]
public record struct SolutionTransferAttemptEvent(EntityUid From, EntityUid To, string? CancelReason = null)
{
    /// <summary>
    /// Cancels the transfer.
    /// </summary>
    public void Cancel(string reason)
    {
        CancelReason = reason;
    }
}

/// <summary>
/// Raised on the target entity when a non-zero amount of solution gets transferred.
/// </summary>
[ByRefEvent]
public record struct SolutionTransferredEvent(EntityUid From, EntityUid To, EntityUid User, FixedPoint2 Amount);
