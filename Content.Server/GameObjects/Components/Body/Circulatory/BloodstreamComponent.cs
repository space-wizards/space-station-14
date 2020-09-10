using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Metabolism;
using Content.Server.Interfaces;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Circulatory
{
    [RegisterComponent]
    public class BloodstreamComponent : Component, IGasMixtureHolder
    {
        public override string Name => "Bloodstream";

        /// <summary>
        ///     Max volume of internal solution storage
        /// </summary>
        [ViewVariables] private ReagentUnit _initialMaxVolume;

        /// <summary>
        ///     Internal solution for reagent storage
        /// </summary>
        [ViewVariables] private SolutionContainerComponent _internalSolution;

        /// <summary>
        ///     Empty volume of internal solution
        /// </summary>
        [ViewVariables] public ReagentUnit EmptyVolume => _internalSolution.EmptyVolume;

        [ViewVariables] public GasMixture Air { get; set; }

        [ViewVariables] public SolutionContainerComponent Solution => _internalSolution;

        public override void Initialize()
        {
            base.Initialize();

            _internalSolution = Owner.EnsureComponent<SolutionContainerComponent>();
            _internalSolution.MaxVolume = _initialMaxVolume;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            Air = new GasMixture(6);

            serializer.DataField(ref _initialMaxVolume, "maxVolume", ReagentUnit.New(250));
        }

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

        public void PumpToxins(GasMixture into, float pressure)
        {
            if (!Owner.TryGetComponent(out MetabolismComponent metabolism))
            {
                Air.PumpGasTo(into, pressure);
                return;
            }

            var toxins = metabolism.Clean(this);

            toxins.PumpGasTo(into, pressure);
            Air.Merge(toxins);
        }
    }
}
