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

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Allows an entity to transfer solutions with a customizable amount per click.
/// Also provides <see cref="Transfer"/> API for other systems.
/// </summary>
public sealed class SolutionTransferSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

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
        var newTransferAmount = FixedPoint2.Clamp(message.Value, ent.Comp.MinimumTransferAmount, ent.Comp.MaximumTransferAmount);
        ent.Comp.TransferAmount = newTransferAmount;

        if (message.Session.AttachedEntity is { Valid: true } user)
            _popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", newTransferAmount)), ent, user);
    }

    private void AddSetTransferVerbs(Entity<SolutionTransferComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var (uid, comp) = ent;

        if (!args.CanAccess || !args.CanInteract || !comp.CanChangeTransferAmount || args.Hands == null)
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        // Custom transfer verb
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("comp-solution-transfer-verb-custom-amount"),
            Category = VerbCategory.SetTransferAmount,
            // TODO: remove server check when bui prediction is a thing
            Act = () =>
            {
                if (_net.IsServer)
                    _ui.TryOpen(uid, TransferAmountUiKey.Key, actor.PlayerSession);
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
            if (transferred > 0)
            {
                var toTheBrim = ownerRefill.AvailableVolume == 0;
                var msg = toTheBrim
                    ? "comp-solution-transfer-fill-fully"
                    : "comp-solution-transfer-fill-normal";

                _popup.PopupClient(Loc.GetString(msg, ("owner", args.Target), ("amount", transferred), ("target", uid)), uid, args.User);

                args.Handled = true;
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

            if (transferred > 0)
            {
                var message = Loc.GetString("comp-solution-transfer-transfer-solution", ("amount", transferred), ("target", target));
                _popup.PopupClient(message, uid, args.User);

                args.Handled = true;
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

        var actualAmount = FixedPoint2.Min(amount, FixedPoint2.Min(sourceSolution.Volume, targetSolution.AvailableVolume));

        var solution = _solution.SplitSolution(source, actualAmount);
        _solution.Refill(targetEntity, target, solution);

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
