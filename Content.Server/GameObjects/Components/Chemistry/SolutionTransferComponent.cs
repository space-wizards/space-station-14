#nullable enable
using System.Threading.Tasks;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
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
                        ? "You fill {0:TheName} to the brim with {1}u from {2:theName}"
                        : "You fill {0:TheName} with {1}u from {2:theName}";

                    target.PopupMessage(eventArgs.User, Loc.GetString(msg, Owner, transferred, target));
                    return true;
                }
            }

            if (CanSend && targetSolution.CanRefill && ownerSolution.CanDrain)
            {
                var transferred = DoTransfer(ownerSolution, targetSolution, TransferAmount, eventArgs.User);

                if (transferred > 0)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("You transfer {0}u to {1:theName}.",
                        transferred, target));

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
                source.Owner.PopupMessage(user, Loc.GetString("{0:TheName} is empty!", source.Owner));
                return ReagentUnit.Zero;
            }

            if (target.RefillSpaceAvailable == 0)
            {
                target.Owner.PopupMessage(user, Loc.GetString("{0:TheName} is full!", target.Owner));
                return ReagentUnit.Zero;
            }

            var actualAmount =
                ReagentUnit.Min(amount, ReagentUnit.Min(source.DrainAvailable, target.RefillSpaceAvailable));

            var solution = source.Drain(actualAmount);
            target.Refill(solution);

            return actualAmount;
        }
    }
}
