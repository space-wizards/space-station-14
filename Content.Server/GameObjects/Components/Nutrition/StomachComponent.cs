using System.Collections.Generic;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class StomachComponent : SharedStomachComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        [ViewVariables(VVAccess.ReadOnly)]
        private SolutionComponent _stomachContents;
        public int MaxVolume
        {
            get => _stomachContents.MaxVolume;
            set => _stomachContents.MaxVolume = value;
        }
        private int _initialMaxVolume;
        //Used to track changes to reagent amounts during metabolism
        private readonly Dictionary<string, int> _reagentDeltas = new Dictionary<string, int>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _initialMaxVolume, "max_volume", 20);
        }

        public override void Initialize()
        {
            base.Initialize();
            //Doesn't use Owner.AddComponent<>() to avoid cross-contamination (e.g. with blood or whatever they holds other solutions)
            _stomachContents = new SolutionComponent();
            _stomachContents.InitializeFromPrototype();
            _stomachContents.MaxVolume = _initialMaxVolume;
            _stomachContents.Owner = Owner; //Manually set owner to avoid crash when VV'ing this
        }

        public bool TryTransferSolution(Solution solution)
        {
            // TODO: For now no partial transfers. Potentially change by design
            if (solution.TotalVolume + _stomachContents.CurrentVolume > _stomachContents.MaxVolume)
            {
                return false;
            }
            _stomachContents.TryAddSolution(solution, false, true);
            return true;
        }

        /// <summary>
        /// Loops through each reagent in _stomachContents, and calls the IMetabolizable for each of them./>
        /// </summary>
        /// <param name="tickTime">The time since the last metabolism tick in seconds.</param>
        public void Metabolize(float tickTime)
        {
            if (_stomachContents.CurrentVolume == 0)
                return;

            //Run metabolism for each reagent, track quantity changes
            _reagentDeltas.Clear();
            foreach (var reagent in _stomachContents.ReagentList)
            {
                if(!_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                    continue;

                foreach (var metabolizable in proto.Metabolism)
                {
                    _reagentDeltas[reagent.ReagentId] = metabolizable.Metabolize(Owner, reagent.ReagentId, tickTime);
                }
            }

            //Apply changes to quantity afterwards. Can't change the reagent quantities while the iterating the
            //list of reagents, because that would invalidate the iterator and throw an exception.
            foreach (var reagentDelta in _reagentDeltas)
            {
                _stomachContents.TryRemoveReagent(reagentDelta.Key, reagentDelta.Value);
            }
        }

        /// <summary>
        /// Triggers metabolism of the reagents inside _stomachContents. Called by <see cref="StomachSystem"/>
        /// </summary>
        /// <param name="tickTime">The time since the last metabolism tick in seconds.</param>
        public void OnUpdate(float tickTime)
        {
            Metabolize(tickTime);
        }
    }
}
