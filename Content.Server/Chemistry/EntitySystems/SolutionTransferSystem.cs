using Content.Shared.Verbs;
using Content.Server.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public sealed class SolutionTransferSystem : EntitySystem
    {
        /// <summary>
        ///     Default transfer amounts for the set-transfer verb.
        /// </summary>
        public static readonly List<int> DefaultTransferAmounts = new() { 1, 5, 10, 25, 50, 100, 250, 500, 1000};

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolutionTransferComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
        }

        private void AddSetTransferVerbs(EntityUid uid, SolutionTransferComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !component.CanChangeTransferAmount || args.Hands == null)
                return;

            if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
                return;

            // Custom transfer verb
            AlternativeVerb custom = new();
            custom.Text = Loc.GetString("comp-solution-transfer-verb-custom-amount");
            custom.Category = VerbCategory.SetTransferAmount;
            custom.Act = () => component.UserInterface?.Open(actor.PlayerSession);
            custom.Priority = 1;
            args.Verbs.Add(custom);

            // Add specific transfer verbs according to the container's size
            var priority = 0;
            foreach (var amount in DefaultTransferAmounts)
            {
                if ( amount < component.MinimumTransferAmount.Int() || amount > component.MaximumTransferAmount.Int())
                    continue;

                AlternativeVerb verb = new();
                verb.Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount));
                verb.Category = VerbCategory.SetTransferAmount;
                verb.Act = () =>
                {
                    component.TransferAmount = FixedPoint2.New(amount);
                    args.User.PopupMessage(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)));
                };

                // we want to sort by size, not alphabetically by the verb text.
                verb.Priority = priority;
                priority--;

                args.Verbs.Add(verb);
            }
        }

        /// <summary>
        /// Transfer from a solution to another.
        /// </summary>
        /// <returns>The actual amount transferred.</returns>
        public FixedPoint2 Transfer(EntityUid user,
            EntityUid sourceEntity,
            Solution source,
            EntityUid targetEntity,
            Solution target,
            FixedPoint2 amount)
        {
            var transferAttempt = new SolutionTransferAttemptEvent(sourceEntity, targetEntity);

            // Check if the source is cancelling the transfer
            RaiseLocalEvent(sourceEntity, transferAttempt, true);
            if (transferAttempt.Cancelled)
            {
                sourceEntity.PopupMessage(user, transferAttempt.CancelReason!);
                return FixedPoint2.Zero;
            }

            if (source.DrainAvailable == 0)
            {
                sourceEntity.PopupMessage(user,
                    Loc.GetString("comp-solution-transfer-is-empty", ("target", sourceEntity)));
                return FixedPoint2.Zero;
            }

            // Check if the target is cancelling the transfer
            RaiseLocalEvent(targetEntity, transferAttempt, true);
            if (transferAttempt.Cancelled)
            {
                sourceEntity.PopupMessage(user, transferAttempt.CancelReason!);
                return FixedPoint2.Zero;
            }

            if (target.AvailableVolume == 0)
            {
                targetEntity.PopupMessage(user,
                    Loc.GetString("comp-solution-transfer-is-full", ("target", targetEntity)));
                return FixedPoint2.Zero;
            }

            var actualAmount = FixedPoint2.Min(amount, FixedPoint2.Min(source.DrainAvailable, target.AvailableVolume));

            var solutionSystem = Get<SolutionContainerSystem>();
            var solution = solutionSystem.Drain(sourceEntity, source, actualAmount);
            solutionSystem.Refill(targetEntity, target, solution);

            return actualAmount;
        }
    }

    /// <summary>
    /// Raised when attempting to transfer from one solution to another.
    /// </summary>
    public sealed class SolutionTransferAttemptEvent : CancellableEntityEventArgs
    {
        public SolutionTransferAttemptEvent(EntityUid from, EntityUid to)
        {
            From = from;
            To = to;
        }

        public EntityUid From { get; }
        public EntityUid To { get; }

        /// <summary>
        /// Why the transfer has been cancelled.
        /// </summary>
        public string? CancelReason { get; private set; }

        /// <summary>
        /// Cancels the transfer.
        /// </summary>
        public void Cancel(string reason)
        {
            base.Cancel();
            CancelReason = reason;
        }
    }
}
