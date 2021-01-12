#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Body.Networks;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Behavior
{
    /// <summary>
    /// Where reagents go when ingested. Tracks ingested reagents over time, and
    /// eventually transfers them to <see cref="SharedBloodstreamComponent"/> once digested.
    /// </summary>
    public class StomachBehavior : MechanismBehavior
    {
        private float _accumulatedFrameTime;

        /// <summary>
        ///     Updates digestion status of ingested reagents.
        ///     Once reagents surpass _digestionDelay they are moved to the
        ///     bloodstream, where they are then metabolized.
        /// </summary>
        /// <param name="frameTime">
        ///     The time since the last update in seconds.
        /// </param>
        public override void Update(float frameTime)
        {
            if (Body == null)
            {
                return;
            }

            _accumulatedFrameTime += frameTime;

            // Update at most once per second
            if (_accumulatedFrameTime < 1)
            {
                return;
            }

            _accumulatedFrameTime -= 1;

            if (!Owner.TryGetComponent(out SharedSolutionContainerComponent? solution) ||
                !Body.Owner.TryGetComponent(out SharedBloodstreamComponent? bloodstream))
            {
                return;
            }

            // Add reagents ready for transfer to bloodstream to transferSolution
            var transferSolution = new Solution();

            // Use ToList here to remove entries while iterating
            foreach (var delta in _reagentDeltas.ToList())
            {
                //Increment lifetime of reagents
                delta.Increment(1);
                if (delta.Lifetime > _digestionDelay)
                {
                    solution.TryRemoveReagent(delta.ReagentId, delta.Quantity);
                    transferSolution.AddReagent(delta.ReagentId, delta.Quantity);
                    _reagentDeltas.Remove(delta);
                }
            }

            // Transfer digested reagents to bloodstream
            bloodstream.TryTransferSolution(transferSolution);
        }

        /// <summary>
        ///     Max volume of internal solution storage
        /// </summary>
        public ReagentUnit MaxVolume
        {
            get => Owner.TryGetComponent(out SharedSolutionContainerComponent? solution) ? solution.MaxVolume : ReagentUnit.Zero;
            set
            {
                if (Owner.TryGetComponent(out SharedSolutionContainerComponent? solution))
                {
                    solution.MaxVolume = value;
                }
            }
        }

        /// <summary>
        ///     Initial internal solution storage volume
        /// </summary>
        [ViewVariables]
        protected ReagentUnit InitialMaxVolume { get; private set; }

        /// <summary>
        ///     Time in seconds between reagents being ingested and them being
        ///     transferred to <see cref="SharedBloodstreamComponent"/>
        /// </summary>
        [ViewVariables]
        private float _digestionDelay;

        /// <summary>
        ///     Used to track how long each reagent has been in the stomach
        /// </summary>
        [ViewVariables]
        private readonly List<ReagentDelta> _reagentDeltas = new();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, s => s.InitialMaxVolume, "maxVolume", ReagentUnit.New(100));
            serializer.DataField(ref _digestionDelay, "digestionDelay", 20);
        }

        public override void Startup()
        {
            base.Startup();

            Owner.EnsureComponentWarn(out SolutionContainerComponent solution);

            solution.MaxVolume = InitialMaxVolume;
        }

        public bool CanTransferSolution(Solution solution)
        {
            if (!Owner.TryGetComponent(out SharedSolutionContainerComponent? solutionComponent))
            {
                return false;
            }

            // TODO: For now no partial transfers. Potentially change by design
            if (!solutionComponent.CanAddSolution(solution))
            {
                return false;
            }

            return true;
        }

        public bool TryTransferSolution(Solution solution)
        {
            if (!CanTransferSolution(solution))
                return false;

            if (!Owner.TryGetComponent(out SharedSolutionContainerComponent? solutionComponent))
            {
                return false;
            }

            // Add solution to _stomachContents
            solutionComponent.TryAddSolution(solution);
            // Add each reagent to _reagentDeltas. Used to track how long each reagent has been in the stomach
            foreach (var reagent in solution.Contents)
            {
                _reagentDeltas.Add(new ReagentDelta(reagent.ReagentId, reagent.Quantity));
            }

            return true;
        }

        /// <summary>
        ///     Used to track quantity changes when ingesting & digesting reagents
        /// </summary>
        protected class ReagentDelta
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
