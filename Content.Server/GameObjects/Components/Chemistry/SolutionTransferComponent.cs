#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.Eui;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    ///     Gives click behavior for transferring to/from other reagent containers.
    /// </summary>
    [RegisterComponent]
    public sealed class SolutionTransferComponent : Component, IAfterInteract
    {
        // Behavior is as such:
        // If it's a reagent tank, TAKE reagent.
        // If it's anything else, GIVE reagent.
        // Of course, only if possible.

        public override string Name => "SolutionTransfer";

        /// <summary>
        ///     The amount of solution to be transferred from this solution when clicking on other solutions with it.
        /// </summary>
        [DataField("transferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount { get; set; } = ReagentUnit.New(5);

        /// <summary>
        ///     The minimum amount of solution that can be transferred at once from this solution.
        /// </summary>
        [DataField("minTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit MinimumTransferAmount { get; set; } = ReagentUnit.New(5);

        /// <summary>
        ///     The maximum amount of solution that can be transferred at once from this solution.
        /// </summary>
        [DataField("maxTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit MaximumTransferAmount { get; set; } = ReagentUnit.New(50);

        /// <summary>
        ///     Can this entity take reagent from reagent tanks?
        /// </summary>
        [DataField("canReceive")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReceive { get; set; } = true;

        /// <summary>
        ///     Can this entity give reagent to other reagent containers?
        /// </summary>
        [DataField("canSend")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanSend { get; set; } = true;

        /// <summary>
        /// Whether you're allowed to change the transfer amount.
        /// </summary>
        [DataField("canChangeTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanChangeTransferAmount { get; set; } = false;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(TransferAmountUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
        }

        public void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            switch (serverMsg.Message)
            {
                case TransferAmountSetValueMessage svm:
                    var amount = Math.Clamp(svm.Value.Int(), MinimumTransferAmount.Int(),
                        MaximumTransferAmount.Int());

                    serverMsg.Session.AttachedEntity?.PopupMessage(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)));
                    SetTransferAmount(ReagentUnit.New(amount));
                    break;
            }
        }

        public void SetTransferAmount(ReagentUnit amount)
        {
            amount = ReagentUnit.New(Math.Clamp(amount.Int(), MinimumTransferAmount.Int(), MaximumTransferAmount.Int()));
            TransferAmount = amount;
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.CanReach || eventArgs.Target == null)
                return false;

            if (!Owner.TryGetComponent(out ISolutionInteractionsComponent? ownerSolution))
                return false;

            var target = eventArgs.Target;
            if (!target.TryGetComponent(out ISolutionInteractionsComponent? targetSolution))
            {
                return false;
            }

            if (CanReceive && target.TryGetComponent(out ReagentTankComponent? tank)
                           && ownerSolution.CanRefill && targetSolution.CanDrain)
            {
                var transferred = DoTransfer(targetSolution, ownerSolution, tank.TransferAmount, eventArgs.User);
                if (transferred > 0)
                {
                    var toTheBrim = ownerSolution.RefillSpaceAvailable == 0;
                    var msg = toTheBrim
                        ? "comp-solution-transfer-fill-fully"
                        : "comp-solution-transfer-fill-normal";

                    target.PopupMessage(eventArgs.User, Loc.GetString(msg, ("take", Owner),
                        ("amount", transferred.Int()),
                        ("give", target)));
                    return true;
                }
            }

            if (CanSend && targetSolution.CanRefill && ownerSolution.CanDrain)
            {
                var transferred = DoTransfer(ownerSolution, targetSolution, TransferAmount, eventArgs.User);

                if (transferred > 0)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-solution-transfer-transfer-solution",
                        ("amount", transferred.Int()),
                        ("solution", target)));

                    return true;
                }
            }

            return true;
        }

        /// <returns>The actual amount transferred.</returns>
        private static ReagentUnit DoTransfer(
            ISolutionInteractionsComponent source,
            ISolutionInteractionsComponent target,
            ReagentUnit amount,
            IEntity user)
        {
            if (source.DrainAvailable == 0)
            {
                source.Owner.PopupMessage(user, Loc.GetString("comp-solution-transfer-is-empty", ("solution", source.Owner)));
                return ReagentUnit.Zero;
            }

            if (target.RefillSpaceAvailable == 0)
            {
                target.Owner.PopupMessage(user, Loc.GetString("comp-solution-transfer-is-full", ("solution", target.Owner)));
                return ReagentUnit.Zero;
            }

            var actualAmount =
                ReagentUnit.Min(amount, ReagentUnit.Min(source.DrainAvailable, target.RefillSpaceAvailable));

            var solution = source.Drain(actualAmount);
            target.Refill(solution);

            return actualAmount;
        }

        [Verb]
        public sealed class TransferAmountVerb : Verb<SolutionTransferComponent>
        {
            protected override void GetData(IEntity user, SolutionTransferComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) || !component.CanChangeTransferAmount)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("comp-solution-transfer-verb-transfer-amount-name", ("amount", component.TransferAmount.Int()));
            }

            protected override void Activate(IEntity user, SolutionTransferComponent component)
            {
                if (!user.TryGetComponent<ActorComponent>(out var actor))
                {
                    return;
                }

                component.UserInterface?.Open(actor.PlayerSession);
            }
        }
    }
}
