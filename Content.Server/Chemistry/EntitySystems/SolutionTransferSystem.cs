using Content.Server.Administration.Logs;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Player;


namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public sealed class SolutionTransferSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        /// <summary>
        ///     Default transfer amounts for the set-transfer verb.
        /// </summary>
        public static readonly List<int> DefaultTransferAmounts = new() { 1, 5, 10, 25, 50, 100, 250, 500, 1000 };

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolutionTransferComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
            SubscribeLocalEvent<SolutionTransferComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<SolutionTransferComponent, TransferAmountSetValueMessage>(OnTransferAmountSetValueMessage);
            SubscribeLocalEvent<SolutionTransferComponent, ComponentGetState>(OnSolutionTransferGetState);
            SubscribeLocalEvent<SolutionTransferComponent, HandSelectedEvent>(OnSolutionTransferHandSelected);
            SubscribeLocalEvent<SolutionTransferComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<SolutionTransferComponent, UseInHandEvent>(OnSolutionContainerUse);
            SubscribeLocalEvent<SolutionTransferComponent, ComponentStartup>(OnSolutionTransferStartup);
        }

        // found solutions for this component
        private void OnSolutionTransferStartup(EntityUid uid, SolutionTransferComponent component, ComponentStartup args)
        {
            if (_solutionContainerSystem.TryGetDrainableSolution(uid, out var ownerDrain))
                component.DrainableSolution = ownerDrain;

            if (EntityManager.TryGetComponent(uid, out RefillableSolutionComponent? refillComp)
                && _solutionContainerSystem.TryGetRefillableSolution(uid, out var ownerRefill, refillable: refillComp))
                component.RefillableSolution = ownerRefill;
        }
        private void OnTransferAmountSetValueMessage(EntityUid uid, SolutionTransferComponent solutionTransfer, TransferAmountSetValueMessage message)
        {
            var newTransferAmount = FixedPoint2.Clamp(message.Value, solutionTransfer.MinimumTransferAmount, solutionTransfer.MaximumTransferAmount);
            solutionTransfer.TransferAmount = newTransferAmount;

            if (message.Session.AttachedEntity is { Valid: true } user)
                _popupSystem.PopupEntity(Loc.GetString("comp-solution-transfer-set-amount",
                    ("amount", newTransferAmount)), uid, user);
        }

        private void AddSetTransferVerbs(EntityUid uid, SolutionTransferComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !component.CanChangeTransferAmount || args.Hands == null)
                return;

            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            // Custom transfer verb
            AlternativeVerb custom = new();
            custom.Text = Loc.GetString("comp-solution-transfer-verb-custom-amount");
            custom.Category = VerbCategory.SetTransferAmount;
            custom.Act = () => _userInterfaceSystem.TryOpen(args.Target, TransferAmountUiKey.Key, actor.PlayerSession);
            custom.Priority = 1;
            args.Verbs.Add(custom);

            // Add specific transfer verbs according to the container's size
            var priority = 0;
            foreach (var amount in DefaultTransferAmounts)
            {
                if (amount < component.MinimumTransferAmount.Int() || amount > component.MaximumTransferAmount.Int())
                    continue;

                AlternativeVerb verb = new();
                verb.Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount));
                verb.Category = VerbCategory.SetTransferAmount;
                verb.Act = () =>
                {
                    component.TransferAmount = FixedPoint2.New(amount);
                    _popupSystem.PopupEntity(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), uid, args.User);
                };

                // we want to sort by size, not alphabetically by the verb text.
                verb.Priority = priority;
                priority--;

                args.Verbs.Add(verb);
            }
        }

        // for updates status control UI
        private void OnSolutionTransferGetState(EntityUid uid, SolutionTransferComponent component,
            ref ComponentGetState args)
        {
            var transferMode = component.ToggleMode;
            var solution = GetTransferModeSolution(uid, component);

            if (solution is Solution modeSolution)
            {
                args.State = new SharedSolutionTransferComponent.SolutionTransferComponentState(
                    modeSolution.Volume, modeSolution.MaxVolume, transferMode);
            }
            else
            {
                args.State = new SharedSolutionTransferComponent.SolutionTransferComponentState(
                    0, 0, transferMode);
            }
        }

        private void OnSolutionTransferHandSelected(EntityUid uid, SolutionTransferComponent component, HandSelectedEvent args)
        {
            SetTransferMode(uid, component);
            Dirty(uid, component);
        }
        private void OnSolutionChange(EntityUid uid, SolutionTransferComponent component, SolutionChangedEvent args)
        {
            SetTransferMode(uid, component, false);
            Dirty(uid, component);
        }

        private void OnAfterInteract(EntityUid uid, SolutionTransferComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            var target = args.Target!.Value;

            var transferAmount = component.TransferAmount;

            var modeSolution = GetTransferModeSolution(uid, component);

            var isTargetDrainableSolution = _solutionContainerSystem.TryGetDrainableSolution(target, out var targetDrainableSolution);

            // if you have inject mode then to spill in something
            if (component.ToggleMode == SharedTransferToggleMode.Inject && modeSolution != null)
            {
                // if target container has refillable component then do it
                if (EntityManager.TryGetComponent(target, out RefillableSolutionComponent? refillTargetComp)
                    && _solutionContainerSystem.TryGetRefillableSolution(target, out var targetRefill, refillable: refillTargetComp))
                {
                    if (EntityManager.TryGetComponent(target, out RefillableSolutionComponent? refill) && refill.MaxRefill != null)
                    {
                        transferAmount = FixedPoint2.Min(transferAmount, (FixedPoint2) refill.MaxRefill);
                    }

                    var transferred = Transfer(args.User, uid, modeSolution, target, targetRefill, transferAmount);

                    if (transferred > 0)
                    {
                        var message = Loc.GetString("comp-solution-transfer-transfer-solution", ("amount", transferred),
                                                                                                        ("target", target));
                        _popupSystem.PopupEntity(message, uid, args.User);
                        args.Handled = true;
                    }
                }
                // we try to spill, but component has not refellable component
                // special for water tank or something like
                else if (isTargetDrainableSolution)
                {
                    var message = Loc.GetString("comp-solution-transfer-no-target-refillable-component", ("target", target));
                    _popupSystem.PopupEntity(message, uid, args.User);
                    args.Handled = true;
                }
            }
            // if you have draw mode than fill from some container
            // of course if target container has a drainable component
            else if (targetDrainableSolution is Solution targetDrain
                        && component.ToggleMode == SharedTransferToggleMode.Draw && modeSolution != null)
            {
                // uid is the entity receiving solution from target.
                if (EntityManager.TryGetComponent(uid, out RefillableSolutionComponent? refill) && refill.MaxRefill != null)
                {
                    transferAmount = FixedPoint2.Min(transferAmount, (FixedPoint2) refill.MaxRefill);
                }

                var transferred = Transfer(args.User, target, targetDrain, uid, modeSolution, transferAmount);

                if (transferred > 0)
                {
                    var toTheBrim = modeSolution.AvailableVolume == 0;
                    var msg = toTheBrim
                        ? "comp-solution-transfer-fill-fully"
                        : "comp-solution-transfer-fill-normal";

                    _popupSystem.PopupEntity(Loc.GetString(msg, ("owner", args.Target), ("amount", transferred), ("target", uid)),
                                                                                                        uid, args.User);
                    args.Handled = true;
                }
            }
        }

        // get available mode from toggle mode(or nextMode) and available solutions
        // if isChangeMode = false, then we don't change toggle mode
        // for example if our bucket is full we change mode for "spill mode"
        // null mode - this is invalid mode and we can't found any solution,
        // you should check solutions for this component
        private SharedTransferToggleMode? GetTransferMode(
                EntityUid uid, SolutionTransferComponent component,
                bool isCanChangeMode = true,
                SharedTransferToggleMode? nextMode = null)
        {
            SharedTransferToggleMode? toggleMode = nextMode ?? component.ToggleMode;

            if (component.DrainableSolution is Solution drainSolution
                    && component.RefillableSolution is Solution refillSolution)
            {
                switch (toggleMode)
                {
                    case SharedTransferToggleMode.Inject:
                        if (isCanChangeMode && drainSolution.Volume == 0)
                            toggleMode = SharedTransferToggleMode.Draw;
                        break;
                    case SharedTransferToggleMode.Draw:
                        if (isCanChangeMode && refillSolution.MaxVolume == refillSolution.Volume)
                            toggleMode = SharedTransferToggleMode.Inject;
                        break;
                    default:
                        if (drainSolution.Volume == drainSolution.MaxVolume)
                            toggleMode = SharedTransferToggleMode.Inject;
                        else
                            toggleMode = SharedTransferToggleMode.Draw;
                        break;
                }
            }
            else
            {
                if (component.DrainableSolution != null)
                    toggleMode = SharedTransferToggleMode.Inject;
                else if (component.RefillableSolution != null)
                    toggleMode = SharedTransferToggleMode.Draw;
                else
                    toggleMode = null;
            }

            return toggleMode;
        }

        private void SetTransferMode(EntityUid uid, SolutionTransferComponent component, bool isCanChangeToggleMode = true)
        {
            component.ToggleMode = GetTransferMode(uid, component, isCanChangeToggleMode);
        }

        // get some solution for current toggleMode
        // for draw - RefillableSolution(pour into the container)
        // fro inject - DrainableSolution(pour out the container)
        private Solution? GetTransferModeSolution(EntityUid uid, SolutionTransferComponent component)
        {
            if (component.ToggleMode == SharedTransferToggleMode.Inject)
                return component.DrainableSolution;

            if (component.ToggleMode == SharedTransferToggleMode.Draw)
                return component.RefillableSolution;

            return null;
        }
        private bool TryGetAvailableNextMode(
                EntityUid uid, SolutionTransferComponent component,
                out SharedTransferToggleMode? nextMode)
        {
            nextMode = SharedTransferToggleMode.Inject;
            if (component.ToggleMode == SharedTransferToggleMode.Inject)
                nextMode = SharedTransferToggleMode.Draw;

            nextMode = GetTransferMode(uid, component, true, nextMode);

            return nextMode is SharedTransferToggleMode mode && mode != component.ToggleMode;
        }

        // try to get change mode with it use in hand
        // for example you can't fill a full bucket
        private void OnSolutionContainerUse(EntityUid uid, SolutionTransferComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            string msg;
            if (TryGetAvailableNextMode(uid, component, out var nextMode))
            {
                var mode = "comp-solution-transfer-set-toggle-mode-draw";
                if (nextMode == SharedTransferToggleMode.Inject)
                    mode = "comp-solution-transfer-set-toggle-mode-inject";

                msg = Loc.GetString(mode, ("fromIn", uid));

                component.ToggleMode = nextMode;
                Dirty(uid, component);
            }
            else
                msg = Loc.GetString("comp-solution-transfer-cant-change-mode");

            _popupSystem.PopupEntity(msg, uid, args.User);
            args.Handled = true;
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
            RaiseLocalEvent(sourceEntity, transferAttempt, broadcast: true);
            if (transferAttempt.Cancelled)
            {
                _popupSystem.PopupEntity(transferAttempt.CancelReason!, sourceEntity, user);
                return FixedPoint2.Zero;
            }

            if (source.Volume == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-solution-transfer-is-empty", ("target", sourceEntity)), sourceEntity, user);
                return FixedPoint2.Zero;
            }

            // Check if the target is cancelling the transfer
            RaiseLocalEvent(targetEntity, transferAttempt, broadcast: true);
            if (transferAttempt.Cancelled)
            {
                _popupSystem.PopupEntity(transferAttempt.CancelReason!, sourceEntity, user);
                return FixedPoint2.Zero;
            }

            if (target.AvailableVolume == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-solution-transfer-is-full", ("target", targetEntity)), targetEntity, user);
                return FixedPoint2.Zero;
            }

            var actualAmount = FixedPoint2.Min(amount, FixedPoint2.Min(source.Volume, target.AvailableVolume));

            var solution = _solutionContainerSystem.Drain(sourceEntity, source, actualAmount);
            _solutionContainerSystem.Refill(targetEntity, target, solution);

            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{EntityManager.ToPrettyString(user):player} transferred {string.Join(", ", solution.Contents)} to {EntityManager.ToPrettyString(targetEntity):entity}, which now contains {string.Join(", ", target.Contents)}");

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
