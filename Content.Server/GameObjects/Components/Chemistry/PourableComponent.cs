using System.Threading.Tasks;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Gives an entity click behavior for pouring reagents into
    /// other entities and being poured into. The entity must have
    /// a SolutionComponent or DrinkComponent for this to work.
    /// (DrinkComponent adds a SolutionComponent if one isn't present).
    /// </summary>
    [RegisterComponent]
    class PourableComponent : Component, IInteractUsing
    {
        public override string Name => "Pourable";

        private ReagentUnit _transferAmount;

        /// <summary>
        ///     The amount of solution to be transferred from this solution when clicking on other solutions with it.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount
        {
            get => _transferAmount;
            set => _transferAmount = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(5.0));
        }

        /// <summary>
        /// Called when the owner of this component is clicked on with another entity.
        /// The owner of this component is the target.
        /// The entity used to click on this one is the attacker.
        /// </summary>
        /// <param name="eventArgs">Attack event args</param>
        /// <returns></returns>
        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            //Get target solution component
            if (!Owner.TryGetComponent<SolutionComponent>(out var targetSolution))
                return false;

            //Get attack solution component
            var attackEntity = eventArgs.Using;
            if (!attackEntity.TryGetComponent<SolutionComponent>(out var attackSolution))
                return false;

            // Calculate possibe solution transfer
            if (targetSolution.CanPourIn && attackSolution.CanPourOut)
            {
                // default logic (beakers and glasses)
                // transfer solution from object in hand to attacked
                return TryTransfer(eventArgs, attackSolution, targetSolution);
            }
            else if (targetSolution.CanPourOut && attackSolution.CanPourIn)
            {
                // storage tanks and sinks logic
                // drain solution from attacked object to object in hand
                return TryTransfer(eventArgs, targetSolution, attackSolution);
            }

            // No transfer possible
            return false;
        }

        bool TryTransfer(InteractUsingEventArgs eventArgs, SolutionComponent fromSolution, SolutionComponent toSolution)
        {
            var fromEntity = fromSolution.Owner;
            if (!fromEntity.TryGetComponent<PourableComponent>(out var fromPourable))
                return false;

            //Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(fromPourable.TransferAmount, toSolution.EmptyVolume);
            if (realTransferAmount <= 0) //Special message if container is full
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Container is full"));
                return false;
            }

            //Move units from attackSolution to targetSolution
            var removedSolution = fromSolution.SplitSolution(realTransferAmount);
            if (!toSolution.TryAddSolution(removedSolution))
                return false;

            Owner.PopupMessage(eventArgs.User, Loc.GetString("Transferred {0}u", removedSolution.TotalVolume));

            return true;
        }
    }
}
