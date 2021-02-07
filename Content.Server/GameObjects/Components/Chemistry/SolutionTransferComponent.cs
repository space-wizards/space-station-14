#nullable enable
using System.Threading.Tasks;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
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

        private ReagentUnit _transferAmount;
        private bool _canReceive;
        private bool _canSend;

        /// <summary>
        ///     The amount of solution to be transferred from this solution when clicking on other solutions with it.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount
        {
            get => _transferAmount;
            set => _transferAmount = value;
        }

        /// <summary>
        ///     Can this entity take reagent from reagent tanks?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReceive
        {
            get => _canReceive;
            set => _canReceive = value;
        }

        /// <summary>
        ///     Can this entity give reagent to other reagent containers?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanSend
        {
            get => _canSend;
            set => _canSend = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(5));
            serializer.DataField(ref _canReceive, "canReceive", true);
            serializer.DataField(ref _canSend, "canSend", true);
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
