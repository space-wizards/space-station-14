using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Metabolism;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Body.Networks;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Circulatory
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBloodstreamComponent))]
    public class BloodstreamComponent : SharedBloodstreamComponent, IGasMixtureHolder
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

            Air = new GasMixture(6) {Temperature = Atmospherics.NormalBodyTemperature};

            serializer.DataField(ref _initialMaxVolume, "maxVolume", ReagentUnit.New(250));
        }

        /// <summary>
        ///     Attempt to transfer provided solution to internal solution.
        ///     Only supports complete transfers
        /// </summary>
        /// <param name="solution">Solution to be transferred</param>
        /// <returns>Whether or not transfer was a success</returns>
        public override bool TryTransferSolution(Solution solution)
        {
            // For now doesn't support partial transfers
            if (solution.TotalVolume + _internalSolution.CurrentVolume > _internalSolution.MaxVolume)
            {
                return false;
            }

            _internalSolution.TryAddSolution(solution, false, true);
            return true;
        }

        public void PumpToxins(GasMixture to)
        {
            if (!Owner.TryGetComponent(out MetabolismComponent metabolism))
            {
                to.Merge(Air);
                Air.Clear();
                return;
            }

            var toxins = metabolism.Clean(this);
            var toOld = to.Gases.ToArray();

            to.Merge(toxins);

            for (var i = 0; i < toOld.Length; i++)
            {
                var newAmount = to.GetMoles(i);
                var oldAmount = toOld[i];
                var delta = newAmount - oldAmount;

                toxins.AdjustMoles(i, -delta);
            }

            Air.Merge(toxins);
        }
    }
}
