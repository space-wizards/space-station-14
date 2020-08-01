using System.Linq;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Network
{
    /// <summary>
    ///     Handles all metabolism for mobs.
    ///     All delivery methods eventually bring reagents to the bloodstream.
    ///     For example, injectors put reagents directly into the bloodstream,
    ///     and the stomach does with some delay.
    /// </summary>
    [UsedImplicitly]
    public class BloodstreamBodyNetwork : BodyNetwork
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        public override string Name => "Bloodstream";

        private float _accumulatedFrameTime;

        /// <summary>
        ///     Max volume of internal solution storage
        /// </summary>
        [ViewVariables] private ReagentUnit _initialMaxVolume;

        /// <summary>
        ///     Internal solution for reagent storage
        /// </summary>
        [ViewVariables] private SolutionComponent _internalSolution;

        /// <summary>
        ///     Empty volume of internal solution
        /// </summary>
        public ReagentUnit EmptyVolume => _internalSolution.EmptyVolume;

        /// <summary>
        ///     Attempt to transfer provided solution to internal solution.
        ///     Only supports complete transfers
        /// </summary>
        /// <param name="solution">Solution to be transferred</param>
        /// <returns>Whether or not transfer was a success</returns>
        public bool TryTransferSolution(Solution solution)
        {
            // For now doesn't support partial transfers
            if (solution.TotalVolume + _internalSolution.CurrentVolume > _internalSolution.MaxVolume)
            {
                return false;
            }

            _internalSolution.TryAddSolution(solution, false, true);
            return true;
        }

        /// <summary>
        ///     Loops through each reagent in _internalSolution,
        ///     and calls <see cref="IMetabolizable.Metabolize"/> for each of them.
        /// </summary>
        /// <param name="tickTime">The time since the last metabolism tick in seconds.</param>
        private void Metabolize(float tickTime)
        {
            if (_internalSolution.CurrentVolume == 0)
            {
                return;
            }

            // Run metabolism for each reagent, remove metabolized reagents
            // Using ToList here lets us edit reagents while iterating
            foreach (var reagent in _internalSolution.ReagentList.ToList())
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _initialMaxVolume, "maxVolume", ReagentUnit.New(250));
        }

        protected override void OnAdd()
        {
            _internalSolution = Owner.GetComponent<SolutionComponent>();
            _internalSolution.MaxVolume = _initialMaxVolume;
        }

        public override void OnRemove()
        {
        }

        /// <summary>
        ///     Triggers metabolism of the reagents inside _internalSolution.
        ///     Called by <see cref="BodySystem.Update"/>
        /// </summary>
        /// <param name="frameTime">The time since the last metabolism tick in seconds.</param>
        public override void Update(float frameTime)
        {
            // Trigger metabolism updates at most once per second
            if (_accumulatedFrameTime < 1.0f)
            {
                _accumulatedFrameTime += frameTime;
                return;
            }

            Metabolize(frameTime);
        }
    }
}
