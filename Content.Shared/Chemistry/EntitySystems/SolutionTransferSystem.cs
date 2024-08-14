using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Allows an entity to transfer solutions with a customizable amount per click.
/// Also provides <see cref="Transfer"/> API for other systems.
/// </summary>
public sealed class SolutionTransferSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _oldSol = default!;
    [Dependency] private readonly SharedSolutionSystem _solutionSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    /// <summary>
    ///     Default transfer amounts for the set-transfer verb.
    /// </summary>
    public static readonly FixedPoint2[] DefaultTransferAmounts = new FixedPoint2[]
    {
        1, 5, 10, 25, 50, 100, 250, 500, 1000
    };

    private EntityQuery<SolutionComponent> _solutionQuery;
    private EntityQuery<SpillableComponent> _spillableQuery;
    private EntityQuery<DrainableSolutionComponent> _drainableQuery;
    private EntityQuery<RefillableSolutionComponent> _refillableQuery;

    public override void Initialize()
    {
        base.Initialize();

        _solutionQuery = EntityManager.GetEntityQuery<SolutionComponent>();

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
        if (!args.CanReach || args.Target is not {} target|| !_solutionQuery.TryComp(target, out var targetSolComp))
            return;

        var (uid, comp) = ent;

        //Special case for reagent tanks, because normally clicking another container will give solution, not take it.
        if (comp.CanReceive
            && !_refillableQuery.HasComp(target) // target must not be refillable (e.g. Reagent Tanks)
            && _drainableQuery.TryComp(target, out var drainable)// target must be drainable
            && _refillableQuery.TryComp(uid, out var refill)
            && _solutionQuery.TryComp(uid, out var solComp))
        {
            var transferAmount = comp.TransferAmount; // This is the player-configurable transfer amount of "uid," not the target reagent tank.

            // if the receiver has a smaller transfer limit, use that instead
            if (refill?.MaxRefill is {} maxRefill)
                transferAmount = FixedPoint2.Min(transferAmount, maxRefill);

            var transferred = Transfer(args.User, (uid, solComp),
                (target, targetSolComp), transferAmount);
            args.Handled = true;
            if (transferred > 0)
            {
                var toTheBrim = solComp.AvailableVolume == 0;
                var msg = toTheBrim
                    ? "comp-solution-transfer-fill-fully"
                    : "comp-solution-transfer-fill-normal";

                _popup.PopupClient(Loc.GetString(msg, ("owner", args.Target), ("amount", transferred), ("target", uid)), uid, args.User);
                return;
            }
        }

        // if target is refillable, and owner is drainable
        if (comp.CanSend
            && _refillableQuery.TryComp(target, out var targetRefill)
            &&  _solutionQuery.TryComp(target, out var targetSol)
            && _solutionQuery.TryComp(uid, out var ownerSol))
        {
            var transferAmount = comp.TransferAmount;

            if (targetRefill?.MaxRefill is {} maxRefill)
                transferAmount = FixedPoint2.Min(transferAmount, maxRefill);

            var transferred = Transfer(args.User, (uid, ownerSol),
                (target, targetSol) , transferAmount);
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
        Entity<SolutionComponent> source,
        Entity<SolutionComponent> target,
        FixedPoint2 amount)
    {
        var transferAttempt = new SolutionTransferAttemptEvent(source, target);

        // Check if the source is cancelling the transfer
        RaiseLocalEvent(source, ref transferAttempt);
        if (transferAttempt.CancelReason is {} reason)
        {
            _popup.PopupClient(reason, source, user);
            return FixedPoint2.Zero;
        }

        if (source.Comp.Volume == 0)
        {
            _popup.PopupClient(Loc.GetString("comp-solution-transfer-is-empty", ("target", source)), source, user);
            return FixedPoint2.Zero;
        }

        // Check if the target is cancelling the transfer
        RaiseLocalEvent(target, ref transferAttempt);
        if (transferAttempt.CancelReason is {} targetReason)
        {
            _popup.PopupClient(targetReason, target, user);
            return FixedPoint2.Zero;
        }
        if (target.Comp.AvailableVolume == 0)
        {
            _popup.PopupClient(Loc.GetString("comp-solution-transfer-is-full", ("target", target)), target, user);
            return FixedPoint2.Zero;
        }

        var actualAmount = FixedPoint2.Min(amount, FixedPoint2.Min(target.Comp.Volume, target.Comp.AvailableVolume));

        var solution = _oldSol.SplitSolution(source, actualAmount);
        _oldSol.AddSolution(target, solution);

        var ev = new SolutionTransferredEvent(source, target, user, actualAmount);
        RaiseLocalEvent(target, ref ev);

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} transferred {SharedSolutionSystem.ToPrettyString(null,solution)} " +
            $"to {ToPrettyString(target):target}, which now contains {ToPrettyString(target)}");

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
