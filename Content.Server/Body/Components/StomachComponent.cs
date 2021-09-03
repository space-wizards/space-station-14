using System.Collections.Generic;
using System.Linq;
using Content.Server.Body.EntitySystems;
using Content.Server.Chemistry.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Content.Shared.Chemistry.Solution.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    /// <summary>
    ///     Where reagents go when ingested. Tracks ingested reagents over time, and
    ///     eventually transfers them to <see cref="SharedBloodstreamComponent"/> once digested.
    /// </summary>
    [RegisterComponent, Friend(typeof(StomachSystem))]
    public class StomachComponent : Component
    {
        public override string Name { get; } = "Stomach";

        public float AccumulatedFrameTime;

        // TODO MIRROR better solution
        [ComponentDependency]
        public MechanismComponent? Mechanism = default!;

        [DataField("updateFrequency")]
        public float UpdateFrequency = 1.0f;

        /// <summary>
        ///     Time in seconds between reagents being ingested and them being
        ///     transferred to <see cref="SharedBloodstreamComponent"/>
        /// </summary>
        [DataField("digestionDelay")] [ViewVariables]
        public float DigestionDelay = 20;

        /// <summary>
        ///     Used to track how long each reagent has been in the stomach
        /// </summary>
        [ViewVariables]
        public readonly List<ReagentDelta> ReagentDeltas = new();

        /// <summary>
        ///     Used to track quantity changes when ingesting & digesting reagents
        /// </summary>
        public class ReagentDelta
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
