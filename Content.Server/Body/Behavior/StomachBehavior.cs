using System.Collections.Generic;
using System.Linq;
using Content.Server.Body.Circulatory;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Body.Networks;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Behavior
{
    /// <summary>
    /// Where reagents go when ingested. Tracks ingested reagents over time, and
    /// eventually transfers them to <see cref="SharedBloodstreamComponent"/> once digested.
    /// </summary>
    public class StomachBehavior : MechanismBehavior
    {
        private const string DefaultSolutionName = "stomach";
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
            // Do not metabolise if the organ does not have a body.
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

            if (!Body.Owner.TryGetComponent(out BloodstreamComponent? bloodstream) ||
                StomachSolution == null) // Something has gone wrong here.
            {
                return;
            }

            // Reagents ready to transfer into the bloodstream are added to this solution
            var transferSolution = new Solution();

            // Use ToList here to remove entries while iterating
            foreach (var delta in _reagentDeltas.ToList())
            {
                //Increment lifetime of reagents
                delta.Increment(1);
                if (delta.Lifetime > _digestionDelay)
                {
                    // This reagent has been in the stomach long enough, TRY to transfer it.
                    // But first, check if the reagent still exists, and how much is left.
                    // Some poor spessman may have washed down a potassium snack with some water.
                    if (StomachSolution.ContainsReagent(delta.ReagentId, out FixedPoint2 quantity))
                    {
                        if (quantity > delta.Quantity)
                        {
                            quantity = delta.Quantity;
                        }

                        EntitySystem.Get<SolutionContainerSystem>()
                            .TryRemoveReagent(Owner.Uid, StomachSolution, delta.ReagentId, quantity);
                        transferSolution.AddReagent(delta.ReagentId, quantity);
                    }

                    _reagentDeltas.Remove(delta);
                }
            }

            // Transfer digested reagents to bloodstream
            bloodstream.TryTransferSolution(transferSolution);
        }

        public Solution? StomachSolution
        {
            get
            {
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, DefaultSolutionName, out var solution);
                return solution;
            }
        }

        /// <summary>
        ///     Max volume of internal solution storage
        /// </summary>
        public FixedPoint2 MaxVolume
        {
            get =>
                StomachSolution?.MaxVolume ?? FixedPoint2.Zero;

            set
            {
                if (StomachSolution != null)
                {
                    StomachSolution.MaxVolume = value;
                }
            }
        }

        /// <summary>
        ///     Initial internal solution storage volume
        /// </summary>
        [DataField("maxVolume")]
        [ViewVariables]
        protected FixedPoint2 InitialMaxVolume { get; private set; } = FixedPoint2.New(100);

        /// <summary>
        ///     Time in seconds between reagents being ingested and them being
        ///     transferred to <see cref="SharedBloodstreamComponent"/>
        /// </summary>
        [DataField("digestionDelay")] [ViewVariables]
        private float _digestionDelay = 20;

        /// <summary>
        ///     Used to track how long each reagent has been in the stomach
        /// </summary>
        [ViewVariables] private readonly List<ReagentDelta> _reagentDeltas = new();

        public override void Startup()
        {
            base.Startup();

            var solution = EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(Owner.Uid, DefaultSolutionName);
            solution.MaxVolume = InitialMaxVolume;
        }

        public bool CanTransferSolution(Solution solution)
        {
            if (StomachSolution == null)
            {
                return false;
            }

            // TODO: For now no partial transfers. Potentially change by design
            if (!StomachSolution.CanAddSolution(solution))
            {
                return false;
            }

            return true;
        }

        public bool TryTransferSolution(Solution solution)
        {
            if (Owner == null || !CanTransferSolution(solution))
                return false;

            if (StomachSolution == null)
            {
                return false;
            }

            // Add solution to _stomachContents
            EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(Owner.Uid, StomachSolution, solution);
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
            public readonly FixedPoint2 Quantity;
            public float Lifetime { get; private set; }

            public ReagentDelta(string reagentId, FixedPoint2 quantity)
            {
                ReagentId = reagentId;
                Quantity = quantity;
                Lifetime = 0.0f;
            }

            public void Increment(float delta) => Lifetime += delta;
        }
    }
}
