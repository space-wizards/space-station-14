using System.Linq;
using Content.Server.Atmos;
using Content.Server.Chemistry.Components;
using Content.Server.Interfaces;
using Content.Server.Metabolism;
using Content.Shared.Atmos;
using Content.Shared.Body.Networks;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Circulatory
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBloodstreamComponent))]
    public class BloodstreamComponent : SharedBloodstreamComponent, IGasMixtureHolder
    {
        public override string Name => "Bloodstream";

        /// <summary>
        ///     Max volume of internal solution storage
        /// </summary>
        [DataField("maxVolume")]
        [ViewVariables] private ReagentUnit _initialMaxVolume = ReagentUnit.New(250);

        /// <summary>
        ///     Internal solution for reagent storage
        /// </summary>
        [ViewVariables] private SolutionContainerComponent _internalSolution = default!;

        /// <summary>
        ///     Empty volume of internal solution
        /// </summary>
        [ViewVariables] public ReagentUnit EmptyVolume => _internalSolution.EmptyVolume;

        [ViewVariables]
        public GasMixture Air { get; set; } = new(6)
            {Temperature = Atmospherics.NormalBodyTemperature};

        [ViewVariables] public SolutionContainerComponent Solution => _internalSolution;

        public override void Initialize()
        {
            base.Initialize();

            _internalSolution = Owner.EnsureComponent<SolutionContainerComponent>();
            _internalSolution.MaxVolume = _initialMaxVolume;
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

            _internalSolution.TryAddSolution(solution);
            return true;
        }

        public void PumpToxins(GasMixture to)
        {
            if (!Owner.TryGetComponent(out MetabolismComponent? metabolism))
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
