using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
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
/// Allows an entity to transfer solutions with a customizable amount per click.
/// Also provides <see cref="Transfer"/> API for other systems.
/// </summary>
public sealed class SolutionTransferSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

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
        SubscribeLocalEvent<SolutionTransferComponent, SolutionDrainTransferDoAfterEvent>(OnSolutionDrainTransferDoAfter);
        SubscribeLocalEvent<SolutionTransferComponent, SolutionFillTransferDoAfterEvent>(OnSolutionFillTransferDoAfter);
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

    private void OnSolutionDrainTransferDoAfter(Entity<SolutionTransferComponent> ent, ref SolutionDrainTransferDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var (uid, comp) = ent;

        if (comp.CanSend
            && args.Target.HasValue
            && TryComp<RefillableSolutionComponent>(args.Target, out var targetRefill)
            && TryComp<DrainableSolutionComponent>(uid, out var drainComp)
            && _solution.TryGetRefillableSolution((args.Target.Value, targetRefill, null), out var targetSoln, out _)
            && _solution.TryGetDrainableSolution((uid, drainComp), out var ownerSoln, out _))
        {
            DrainTransfer(args.User, uid, ownerSoln.Value, args.Target.Value, targetSoln.Value, args.Amount);
        }
    }

    private void OnSolutionFillTransferDoAfter(Entity<SolutionTransferComponent> ent, ref SolutionFillTransferDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var (uid, comp) = ent;

        if (comp.CanReceive
            && args.Target.HasValue
            && !HasComp<RefillableSolutionComponent>(args.Target.Value) // target must not be refillable (e.g. Reagent Tanks)
            && _solution.TryGetDrainableSolution(args.Target.Value, out var targetSoln, out _) // target must be drainable
            && TryComp<RefillableSolutionComponent>(uid, out var refill)
            && _solution.TryGetRefillableSolution((uid, refill, null), out var ownerSoln, out var ownerRefill))
        {
            FillTransfer(args.User, args.Target.Value, targetSoln.Value, uid, ownerSoln.Value, ownerRefill, args.Amount);
        }
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
          if (amount < comp.MinimumTransferAmount || amount > comp.MaximumTransferAmount)
                continue;

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

            var transferTime = refill?.RefillTime;
            if (transferTime > 0)
            {
                if (!CanTransfer(args.User, target, targetSoln.Value, uid, ownerSoln.Value, transferAmount))
                    return;

                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, transferTime.Value, new SolutionFillTransferDoAfterEvent(transferAmount), uid, target, null)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                    Hidden = true,
                };
                _doAfter.TryStartDoAfter(doAfterArgs, null);
                args.Handled = true;
                return;
            }
            else
            {
                var transferred = FillTransfer(args.User, target, targetSoln.Value, uid, ownerSoln.Value, ownerRefill, transferAmount);
                if (transferred > 0)
                {
                    args.Handled = true;
                    return;
                }
            }
        }

        // if target is refillable, and owner is drainable
        if (comp.CanSend
            && TryComp<RefillableSolutionComponent>(target, out var targetRefill)
            && TryComp<DrainableSolutionComponent>(uid, out var drainComp)
            && _solution.TryGetRefillableSolution((target, targetRefill, null), out targetSoln, out _)
            && _solution.TryGetDrainableSolution((uid, drainComp), out ownerSoln, out _))
        {
            var transferAmount = comp.TransferAmount;

            if (targetRefill?.MaxRefill is {} maxRefill)
                transferAmount = FixedPoint2.Min(transferAmount, maxRefill);

            var transferTime = targetRefill?.RefillTime + drainComp.DrainTime;
            if (transferTime > 0)
            {
                if (!CanTransfer(args.User, uid, ownerSoln.Value, target, targetSoln.Value, transferAmount))
                    return;

                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, transferTime.Value, new SolutionDrainTransferDoAfterEvent(transferAmount), uid, target, null)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                    Hidden = true,
                };
                _doAfter.TryStartDoAfter(doAfterArgs, null);
            }
            else
            {
                DrainTransfer(args.User, uid, ownerSoln.Value, target, targetSoln.Value, transferAmount);
            }
            args.Handled = true;
        }
    }

    /// <summary>
    /// Attempt to drain a solution into another, such as pouring a bottle into a glass.
    /// </summary>
    /// <param name="user">The user performing the action.</param>
    /// <param name="sourceEntity">The entity being filled.</param>
    /// <param name="source">The solution entity being filled.</param>
    /// <param name="targetEntity">The entity being drained from.</param>
    /// <param name="target">The solution entity being drained from.</param>
    /// <param name="amount">The amount being transferred.</param>
    /// <returns>The amount that finally got transferred.</returns>
    private FixedPoint2 DrainTransfer(EntityUid user,
        EntityUid sourceEntity,
        Entity<SolutionComponent> source,
        EntityUid targetEntity,
        Entity<SolutionComponent> target,
        FixedPoint2 amount)
    {
        var transferred = Transfer(user, sourceEntity, source, targetEntity, target, amount);
        if (transferred > 0)
        {
            var message = Loc.GetString("comp-solution-transfer-transfer-solution", ("amount", transferred), ("target", targetEntity));
            _popup.PopupClient(message, sourceEntity, user);
        }

        return transferred;
    }

    /// <summary>
    /// Attempt to fill a solution from another container, such as tanks.
    /// </summary>
    /// <param name="user">The user performing the action.</param>
    /// <param name="sourceEntity">The entity being drained from.</param>
    /// <param name="source">The solution entity being drained from.</param>
    /// <param name="targetEntity">The entity being filled.</param>
    /// <param name="target">The solution entity being filled.</param>
    /// <param name="targetSolution">The solution being filled.</param>
    /// <param name="amount">The amount being transferred.</param>
    /// <returns>The amount that finally got transferred.</returns>
    private FixedPoint2 FillTransfer(EntityUid user,
        EntityUid sourceEntity,
        Entity<SolutionComponent> source,
        EntityUid targetEntity,
        Entity<SolutionComponent> target,
        Solution targetSolution,
        FixedPoint2 amount)
    {
        var transferred = Transfer(user, sourceEntity, source, targetEntity, target, amount);
        if (transferred > 0)
        {
            var toTheBrim = targetSolution.AvailableVolume == 0;
            var msg = toTheBrim
                ? "comp-solution-transfer-fill-fully"
                : "comp-solution-transfer-fill-normal";

            _popup.PopupClient(Loc.GetString(msg, ("owner", sourceEntity), ("amount", transferred), ("target", targetEntity)), targetEntity, user);
        }

        return transferred;
    }

    /// <summary>
    /// Check if the source solution can transfer the amount to the target solution, and display a pop-up if it fails.
    /// </summary>
    public bool CanTransfer(EntityUid user,
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
            return false;
        }

        var sourceSolution = source.Comp.Solution;
        if (sourceSolution.Volume == 0)
        {
            _popup.PopupClient(Loc.GetString("comp-solution-transfer-is-empty", ("target", sourceEntity)), sourceEntity, user);
            return false;
        }

        // Check if the target is cancelling the transfer
        RaiseLocalEvent(targetEntity, ref transferAttempt);
        if (transferAttempt.CancelReason is {} targetReason)
        {
            _popup.PopupClient(targetReason, targetEntity, user);
            return false;
        }

        var targetSolution = target.Comp.Solution;
        if (targetSolution.AvailableVolume == 0)
        {
            _popup.PopupClient(Loc.GetString("comp-solution-transfer-is-full", ("target", targetEntity)), targetEntity, user);
            return false;
        }

        return true;
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
        var sourceSolution = source.Comp.Solution;
        var targetSolution = target.Comp.Solution;

        if (!CanTransfer(user, sourceEntity, source, targetEntity, target, amount))
            return FixedPoint2.Zero;

        var actualAmount = FixedPoint2.Min(amount, FixedPoint2.Min(sourceSolution.Volume, targetSolution.AvailableVolume));

        var solution = _solution.SplitSolution(source, actualAmount);
        _solution.AddSolution(target, solution);

        var ev = new SolutionTransferredEvent(sourceEntity, targetEntity, user, actualAmount);
        RaiseLocalEvent(targetEntity, ref ev);

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} transferred {SharedSolutionContainerSystem.ToPrettyString(solution)} to {ToPrettyString(targetEntity):target}, which now contains {SharedSolutionContainerSystem.ToPrettyString(targetSolution)}");

        return actualAmount;
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
public sealed partial class SolutionFillTransferDoAfterEvent : DoAfterEvent
{
    public FixedPoint2 Amount;

    public SolutionFillTransferDoAfterEvent(FixedPoint2 amount)
    {
        Amount = amount;
    }

    public override DoAfterEvent Clone() => this;
}
