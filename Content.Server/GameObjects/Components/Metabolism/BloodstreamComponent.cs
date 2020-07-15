using System.Linq;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Metabolism
{
    /// <summary>
    /// Handles all metabolism for mobs. All delivery methods eventually bring reagents
    /// to the bloodstream. For example, injectors put reagents directly into the bloodstream,
    /// and the stomach does with some delay.
    /// </summary>
    [RegisterComponent]
    public class BloodstreamComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        public override string Name => "Bloodstream";

        /// <summary>
        /// Internal solution for reagent storage
        /// </summary>
        [ViewVariables]
        private SolutionComponent _internalSolution;

        /// <summary>
        /// Max volume of internal solution storage
        /// </summary>
        [ViewVariables]
        private ReagentUnit _initialMaxVolume;

        /// <summary>
        /// Empty volume of internal solution
        /// </summary>
        public ReagentUnit EmptyVolume => _internalSolution.EmptyVolume;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _initialMaxVolume, "maxVolume", ReagentUnit.New(250));
        }

        protected override void Startup()
        {
            base.Startup();
            _internalSolution = Owner.GetComponent<SolutionComponent>();
            _internalSolution.MaxVolume = _initialMaxVolume;
        }

        /// <summary>
        /// Attempt to transfer provided solution to internal solution. Only supports complete transfers
        /// </summary>
        /// <param name="solution">Solution to be transferred</param>
        /// <returns>Whether or not transfer was a success</returns>
        public bool TryTransferSolution(Solution solution)
        {
            //For now doesn't support partial transfers
            if (solution.TotalVolume + _internalSolution.CurrentVolume > _internalSolution.MaxVolume)
            {
                return false;
            }

            _internalSolution.TryAddSolution(solution, false, true);
            return true;
        }

        /// <summary>
        /// Loops through each reagent in _internalSolution, and calls the IMetabolizable for each of them./>
        /// </summary>
        /// <param name="tickTime">The time since the last metabolism tick in seconds.</param>
        private void Metabolize(float tickTime)
        {
            if (_internalSolution.CurrentVolume == 0)
            {
                return;
            }

            //Run metabolism for each reagent, remove metabolized reagents
            foreach (var reagent in _internalSolution.ReagentList.ToList()) //Using ToList here lets us edit reagents while iterating
            {
                if (!_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                {
                    continue;
                }

                //Run metabolism code for each reagent
                foreach (var metabolizable in proto.Metabolism)
                {
                    var reagentDelta = metabolizable.Metabolize(Owner, reagent.ReagentId, tickTime);
                    _internalSolution.TryRemoveReagent(reagent.ReagentId, reagentDelta);
                }
            }
        }

        /// <summary>
        /// Triggers metabolism of the reagents inside _internalSolution. Called by <see cref="BloodstreamSystem"/>
        /// </summary>
        /// <param name="tickTime">The time since the last metabolism tick in seconds.</param>
        public void OnUpdate(float tickTime)
        {
            Metabolize(tickTime);
        }
    }
}
