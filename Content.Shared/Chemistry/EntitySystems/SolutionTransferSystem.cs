using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Allows an entity to transfer solutions with a customizable amount -per click-.
/// Also provides <see cref="Transfer"/>, <see cref="RefillTransfer"/> and <see cref="DrainTransfer"/> API for other systems.
/// </summary>
public sealed class SolutionTransferSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    private EntityQuery<RefillableSolutionComponent> _refillableQuery;
    private EntityQuery<DrainableSolutionComponent> _drainableQuery;

    /// <summary>
    ///     Default transfer amounts for the set-transfer verb.
    /// </summary>
    public static readonly FixedPoint2[] DefaultTransferAmounts = new FixedPoint2[] { 1, 5, 10, 25, 50, 100, 250, 500, 1000 };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionTransferComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
        SubscribeLocalEvent<SolutionTransferComponent, TransferAmountSetValueMessage>(OnTransferAmountSetValueMessage);
        SubscribeLocalEvent<SolutionTransferComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SolutionTransferComponent, SolutionDrainTransferDoAfterEvent>(OnSolutionDrainTransferDoAfter);
        SubscribeLocalEvent<SolutionTransferComponent, SolutionRefillTransferDoAfterEvent>(OnSolutionFillTransferDoAfter);

        _refillableQuery = GetEntityQuery<RefillableSolutionComponent>();
        _drainableQuery = GetEntityQuery<DrainableSolutionComponent>();
    }

    private void AddSetTransferVerbs(Entity<SolutionTransferComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !ent.Comp.CanChangeTransferAmount || args.Hands == null)
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
                _ui.OpenUi(ent.Owner, TransferAmountUiKey.Key, @event.User);
            },
            Priority = 1
        });

        // Add specific transfer verbs according to the container's size
        var priority = 0;
        var user = args.User;
        foreach (var amount in DefaultTransferAmounts)
        {
            if (amount < ent.Comp.MinimumTransferAmount || amount > ent.Comp.MaximumTransferAmount)
                continue;

            AlternativeVerb verb = new();
            verb.Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount));
            verb.Category = VerbCategory.SetTransferAmount;
            verb.Act = () =>
            {
                ent.Comp.TransferAmount = amount;

                _popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), ent.Owner, user);

                Dirty(ent.Owner, ent.Comp);
            };

            // we want to sort by size, not alphabetically by the verb text.
            verb.Priority = priority;
            priority--;

            args.Verbs.Add(verb);
        }
    }

    private void OnTransferAmountSetValueMessage(Entity<SolutionTransferComponent> ent, ref TransferAmountSetValueMessage message)
    {
        var newTransferAmount = FixedPoint2.Clamp(message.Value, ent.Comp.MinimumTransferAmount, ent.Comp.MaximumTransferAmount);
        ent.Comp.TransferAmount = newTransferAmount;

        if (message.Actor is { Valid: true } user)
            _popup.PopupEntity(Loc.GetString("comp-solution-transfer-set-amount", ("amount", newTransferAmount)), ent.Owner, user);

        Dirty(ent.Owner, ent.Comp);
    }

    private void OnAfterInteract(Entity<SolutionTransferComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not {} target)
            return;

        // We have two cases for interaction:
        // Held Drainable --> Target Refillable
        // Held Refillable <-- Target Drainable

        // In the case where the target has both Refillable and Drainable, Held --> Target takes priority.

        if (ent.Comp.CanSend
            && _drainableQuery.TryComp(ent.Owner, out var heldDrainable)
            && _refillableQuery.TryComp(target, out var targetRefillable)
            && TryGetTransferrableSolutions((ent.Owner, heldDrainable),
                (target, targetRefillable),
                out var ownerSoln,
                out var targetSoln,
                out _))
        {
            args.Handled = true; //If we reach this point, the interaction counts as handled.

            var transferAmount = ent.Comp.TransferAmount;
            if (targetRefillable.MaxRefill is {} maxRefill)
                transferAmount = FixedPoint2.Min(transferAmount, maxRefill);

            var transferData = new SolutionTransferData(args.User, ent.Owner, ownerSoln.Value, target, targetSoln.Value, transferAmount);
            var transferTime = targetRefillable.RefillTime + heldDrainable.DrainTime;

            if (transferTime > TimeSpan.Zero)
            {
                if (!CanTransfer(transferData))
                    return;

                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, transferTime, new SolutionDrainTransferDoAfterEvent(transferAmount), ent.Owner, target)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                    Hidden = true,
                };
                _doAfter.TryStartDoAfter(doAfterArgs);
            }
            else
            {
                DrainTransfer(transferData);
            }

            return;
        }

        if (ent.Comp.CanReceive
            && _refillableQuery.TryComp(ent.Owner, out var heldRefillable)
            && _drainableQuery.TryComp(target, out var targetDrainable)
            && TryGetTransferrableSolutions((target, targetDrainable),
                (ent.Owner, heldRefillable),
                out targetSoln,
                out ownerSoln,
                out var solution))
        {
            args.Handled = true; //If we reach this point, the interaction counts as handled.

            var transferAmount = ent.Comp.TransferAmount; // This is the player-configurable transfer amount of "uid," not the target drainable.
            if (heldRefillable.MaxRefill is {} maxRefill) // if the receiver has a smaller transfer limit, use that instead
                transferAmount = FixedPoint2.Min(transferAmount, maxRefill);

            var transferData = new SolutionTransferData(args.User, target, targetSoln.Value, ent.Owner, ownerSoln.Value, transferAmount);
            var transferTime = heldRefillable.RefillTime + targetDrainable.DrainTime;

            if (transferTime > TimeSpan.Zero)
            {
                if (!CanTransfer(transferData))
                    return;

                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, transferTime, new SolutionRefillTransferDoAfterEvent(transferAmount), ent.Owner, target)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                    Hidden = true,
                };
                _doAfter.TryStartDoAfter(doAfterArgs);
            }
            else
            {
                RefillTransfer(transferData, solution);
            }
        }
    }

    private void OnSolutionDrainTransferDoAfter(Entity<SolutionTransferComponent> ent, ref SolutionDrainTransferDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        // Have to check again, in case something has changed.
        if (CanSend(ent, target, out var ownerSoln, out var targetSoln))
        {
            DrainTransfer(new SolutionTransferData(args.User, ent.Owner, ownerSoln.Value, args.Target.Value, targetSoln.Value, args.Amount));
        }
    }

    private void OnSolutionFillTransferDoAfter(Entity<SolutionTransferComponent> ent, ref SolutionRefillTransferDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        // Have to check again, in case something has changed.
        if (!CanRecieve(ent, target, out var ownerSoln, out var targetSoln, out var solution))
            return;

        RefillTransfer(new SolutionTransferData(args.User, target, targetSoln.Value, ent.Owner, ownerSoln.Value, args.Amount), solution);
    }

    private bool CanSend(Entity<SolutionTransferComponent, DrainableSolutionComponent?> ent,
        Entity<RefillableSolutionComponent?> target,
        [NotNullWhen(true)] out Entity<SolutionComponent>? drainable,
        [NotNullWhen(true)] out Entity<SolutionComponent>? refillable)
    {
        drainable = null;
        refillable = null;

        return ent.Comp1.CanReceive && TryGetTransferrableSolutions(ent.Owner, target, out drainable, out refillable, out _);
    }

    private bool CanRecieve(Entity<SolutionTransferComponent> ent,
        EntityUid source,
        [NotNullWhen(true)] out Entity<SolutionComponent>? drainable,
        [NotNullWhen(true)] out Entity<SolutionComponent>? refillable,
        [NotNullWhen(true)] out Solution? solution)
    {
        drainable = null;
        refillable = null;
        solution = null;

        return ent.Comp.CanReceive && TryGetTransferrableSolutions(source, ent.Owner, out drainable, out refillable, out solution);
    }

    private bool TryGetTransferrableSolutions(Entity<DrainableSolutionComponent?> source,
        Entity<RefillableSolutionComponent?> target,
        [NotNullWhen(true)] out Entity<SolutionComponent>? drainable,
        [NotNullWhen(true)] out Entity<SolutionComponent>? refillable,
        [NotNullWhen(true)] out Solution? solution)
    {
        drainable = null;
        refillable = null;
        solution = null;

        if (!_drainableQuery.Resolve(source, ref source.Comp) || !_refillableQuery.Resolve(target, ref target.Comp))
            return false;

        if (!_solution.TryGetDrainableSolution(source, out drainable, out _))
            return false;

        if (!_solution.TryGetRefillableSolution(target, out refillable, out solution))
            return false;

        return true;
    }

    /// <summary>
    /// Attempt to drain a solution into another, such as pouring a bottle into a glass.
    /// Includes a pop-up if the transfer failed or succeeded
    /// </summary>
    /// <param name="data">The transfer data making up the transfer.</param>
    /// <returns>The actual amount transferred.</returns>
    private void DrainTransfer(SolutionTransferData data)
    {
        var transferred = Transfer(data);
        if (transferred <= 0)
            return;

        var message = Loc.GetString("comp-solution-transfer-transfer-solution", ("amount", transferred), ("target", data.TargetEntity));
        _popup.PopupClient(message, data.SourceEntity, data.User);
    }

    /// <summary>
    /// Attempt to fill a solution from another container, such as tapping from a water tank.
    /// Includes a pop-up if the transfer failed or succeeded.
    /// </summary>
    /// <param name="data">The transfer data making up the transfer.</param>
    /// <param name="targetSolution">The target solution,included for LoC pop-up purposes.</param>
    /// <returns>The actual amount transferred.</returns>
    private void RefillTransfer(SolutionTransferData data, Solution targetSolution)
    {
        var transferred = Transfer(data);
        if (transferred <= 0)
            return;

        var toTheBrim = targetSolution.AvailableVolume == 0;
        var msg = toTheBrim
            ? "comp-solution-transfer-fill-fully"
            : "comp-solution-transfer-fill-normal";

        _popup.PopupClient(Loc.GetString(msg, ("owner", data.SourceEntity), ("amount", transferred), ("target", data.TargetEntity)), data.TargetEntity, data.User);
    }

    /// <summary>
    /// Transfer from a solution to another, allowing either entity to cancel.
    /// Includes a pop-up if the transfer failed.
    /// </summary>
    /// <returns>The actual amount transferred.</returns>
    public FixedPoint2 Transfer(SolutionTransferData data)
    {
        var sourceSolution = data.Source.Comp.Solution;
        var targetSolution = data.Target.Comp.Solution;

        if (!CanTransfer(data))
            return FixedPoint2.Zero;

        var actualAmount = FixedPoint2.Min(data.Amount, FixedPoint2.Min(sourceSolution.Volume, targetSolution.AvailableVolume));

        var solution = _solution.SplitSolution(data.Source, actualAmount);
        _solution.AddSolution(data.Target, solution);

        var ev = new SolutionTransferredEvent(data.SourceEntity, data.TargetEntity, data.User, actualAmount);
        RaiseLocalEvent(data.TargetEntity, ref ev);

        _adminLogger.Add(LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(data.User):player} transferred {SharedSolutionContainerSystem.ToPrettyString(solution)} to {ToPrettyString(data.TargetEntity):target}, which now contains {SharedSolutionContainerSystem.ToPrettyString(targetSolution)}");

        return actualAmount;
    }

    /// <summary>
    /// Check if the source solution can transfer the amount to the target solution, and display a pop-up if it fails.
    /// </summary>
    private bool CanTransfer(SolutionTransferData data)
    {
        var transferAttempt = new SolutionTransferAttemptEvent(data.SourceEntity, data.TargetEntity);

        // Check if the source is cancelling the transfer
        RaiseLocalEvent(data.SourceEntity, ref transferAttempt);
        if (transferAttempt.CancelReason is {} reason)
        {
            _popup.PopupClient(reason, data.SourceEntity, data.User);
            return false;
        }

        var sourceSolution = data.Source.Comp.Solution;
        if (sourceSolution.Volume == 0)
        {
            _popup.PopupClient(Loc.GetString("comp-solution-transfer-is-empty", ("target", data.SourceEntity)), data.SourceEntity, data.User);
            return false;
        }

        // Check if the target is cancelling the transfer
        RaiseLocalEvent(data.TargetEntity, ref transferAttempt);
        if (transferAttempt.CancelReason is {} targetReason)
        {
            _popup.PopupClient(targetReason, data.TargetEntity, data.User);
            return false;
        }

        var targetSolution = data.Target.Comp.Solution;
        if (targetSolution.AvailableVolume == 0)
        {
            _popup.PopupClient(Loc.GetString("comp-solution-transfer-is-full", ("target", data.TargetEntity)), data.TargetEntity, data.User);
            return false;
        }

        return true;
    }
}


/// <summary>
/// A collection of data containing relevant entities and values for transferring reagents.
/// </summary>
/// <param name="user">The user performing the transfer.</param>
/// <param name="sourceEntity">The entity holding the solution container which reagents are being moved from.</param>
/// <param name="source">The entity holding the solution from which reagents are being moved away from.</param>
/// <param name="targetEntity">The entity holding the solution container which reagents are being moved to.</param>
/// <param name="target">The entity holding the solution which reagents are being moved to</param>
/// <param name="amount">The amount being moved.</param>
public struct SolutionTransferData(EntityUid user, EntityUid sourceEntity, Entity<SolutionComponent> source, EntityUid targetEntity, Entity<SolutionComponent> target, FixedPoint2 amount)
{
    public EntityUid User = user;
    public EntityUid SourceEntity = sourceEntity;
    public Entity<SolutionComponent> Source = source;
    public EntityUid TargetEntity = targetEntity;
    public Entity<SolutionComponent> Target = target;
    public FixedPoint2 Amount = amount;
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

/// <summary>
/// Doafter event for solution transfers where the held item is drained into the target. Checks for validity both when initiating and when finishing the event.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SolutionDrainTransferDoAfterEvent : DoAfterEvent
{
    public FixedPoint2 Amount;

    public SolutionDrainTransferDoAfterEvent(FixedPoint2 amount)
    {
        Amount = amount;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// Doafter event for solution transfers where the held item is filled from the target. Checks for validity both when initiating and when finishing the event.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SolutionRefillTransferDoAfterEvent : DoAfterEvent
{
    public FixedPoint2 Amount;

    public SolutionRefillTransferDoAfterEvent(FixedPoint2 amount)
    {
        Amount = amount;
    }

    public override DoAfterEvent Clone() => this;
}
