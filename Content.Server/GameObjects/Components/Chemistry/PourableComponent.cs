using System;
using System.Collections.Generic;
using System.Text;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Utility;

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
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        public override string Name => "Pourable";

        private ReagentUnit _transferAmount;

        /// <summary>
        ///     The amount of solution to be transferred from this solution when clicking on other solutions with it.
        /// </summary>
        [ViewVariables]
        public ReagentUnit TransferAmount
        {
            get => _transferAmount;
            set => _transferAmount = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(5.0M));
        }

        /// <summary>
        /// Called when the owner of this component is clicked on with another entity.
        /// The owner of this component is the target.
        /// The entity used to click on this one is the attacker.
        /// </summary>
        /// <param name="eventArgs">Attack event args</param>
        /// <returns></returns>
        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            //Get target and check if it can be poured into
            if (!Owner.TryGetComponent<SolutionComponent>(out var targetSolution))
                return false;
            if (!targetSolution.CanPourIn)
                return false;

            //Get attack entity and check if it can pour out.
            var attackEntity = eventArgs.Using;
            if (!attackEntity.TryGetComponent<SolutionComponent>(out var attackSolution) || !attackSolution.CanPourOut)
                return false;
            if (!attackEntity.TryGetComponent<PourableComponent>(out var attackPourable))
                return false;

            //Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(attackPourable.TransferAmount, targetSolution.EmptyVolume);
            if (realTransferAmount <= 0) //Special message if container is full
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, eventArgs.User,
                    _localizationManager.GetString("Container is full"));
                return false;
            }

            //Move units from attackSolution to targetSolution
            var removedSolution = attackSolution.SplitSolution(realTransferAmount);
            if (!targetSolution.TryAddSolution(removedSolution))
                return false;

            _notifyManager.PopupMessage(Owner.Transform.GridPosition, eventArgs.User,
                _localizationManager.GetString("Transferred {0}u", removedSolution.TotalVolume));

            return true;
        }
    }
}
