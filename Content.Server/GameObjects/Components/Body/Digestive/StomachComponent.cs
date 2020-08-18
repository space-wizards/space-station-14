using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Digestive
{
    /// <summary>
    /// Where reagents go when ingested. Tracks ingested reagents over time, and
    /// eventually transfers them to <see cref="BloodstreamComponent"/> once digested.
    /// </summary>
    [RegisterComponent]
    public class StomachComponent : SharedStomachComponent
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        /// <summary>
        ///     Max volume of internal solution storage
        /// </summary>
        public ReagentUnit MaxVolume
        {
            get => _stomachContents.MaxVolume;
            set => _stomachContents.MaxVolume = value;
        }

        /// <summary>
        ///     Internal solution storage
        /// </summary>
        [ViewVariables]
        private SolutionComponent _stomachContents;

        /// <summary>
        ///     Initial internal solution storage volume
        /// </summary>
        [ViewVariables]
        private ReagentUnit _initialMaxVolume;

        /// <summary>
        ///     Time in seconds between reagents being ingested and them being transferred
        ///     to <see cref="BloodstreamComponent"/>
        /// </summary>
        [ViewVariables]
        private float _digestionDelay;

        /// <summary>
        ///     Used to track how long each reagent has been in the stomach
        /// </summary>
        private readonly List<ReagentDelta> _reagentDeltas = new List<ReagentDelta>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _initialMaxVolume, "maxVolume", ReagentUnit.New(100));
            serializer.DataField(ref _digestionDelay, "digestionDelay", 20);
        }

        protected override void Startup()
        {
            base.Startup();

            _stomachContents = Owner.GetComponent<SolutionComponent>();
            _stomachContents.MaxVolume = _initialMaxVolume;
        }

        public bool TryTransferSolution(Solution solution)
        {
            // TODO: For now no partial transfers. Potentially change by design
            if (solution.TotalVolume + _stomachContents.CurrentVolume > _stomachContents.MaxVolume)
            {
                return false;
            }

            // Add solution to _stomachContents
            _stomachContents.TryAddSolution(solution, false, true);
            // Add each reagent to _reagentDeltas. Used to track how long each reagent has been in the stomach
            foreach (var reagent in solution.Contents)
            {
                _reagentDeltas.Add(new ReagentDelta(reagent.ReagentId, reagent.Quantity));
            }

            return true;
        }

        /// <summary>
        ///     Updates digestion status of ingested reagents.
        ///     Once reagents surpass _digestionDelay they are moved to the bloodstream,
        ///     where they are then metabolized.
        /// </summary>
        /// <param name="frameTime">The time since the last update in seconds.</param>
        public void Update(float frameTime)
        {
            if (!Owner.TryGetComponent(out BloodstreamComponent bloodstream))
            {
                return;
            }

            // Add reagents ready for transfer to bloodstream to transferSolution
            var transferSolution = new Solution();

            // Use ToList here to remove entries while iterating
            foreach (var delta in _reagentDeltas.ToList())
            {
                //Increment lifetime of reagents
                delta.Increment(frameTime);
                if (delta.Lifetime > _digestionDelay)
                {
                    _stomachContents.TryRemoveReagent(delta.ReagentId, delta.Quantity);
                    transferSolution.AddReagent(delta.ReagentId, delta.Quantity);
                    _reagentDeltas.Remove(delta);
                }
            }

            // Transfer digested reagents to bloodstream
            bloodstream.TryTransferSolution(transferSolution);
        }

        /// <summary>
        ///     Used to track quantity changes when ingesting & digesting reagents
        /// </summary>
        private class ReagentDelta
        {
            public readonly string ReagentId;
            public readonly ReagentUnit Quantity;
            public float Lifetime { get; private set; }

            public ReagentDelta(string reagentId, ReagentUnit quantity)
            {
                ReagentId = reagentId;
                Quantity = quantity;
                Lifetime = 0.0f;
            }

            public void Increment(float delta) => Lifetime += delta;
        }
    }
}
