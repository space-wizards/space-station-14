using Content.Server.Body.Systems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Access(typeof(StomachSystem), typeof(FoodSystem))]
    public sealed partial class StomachComponent : Component
    {
        public float AccumulatedFrameTime;

        /// <summary>
        ///     How fast should this component update, in seconds?
        /// </summary>
        [DataField]
        public float UpdateInterval = 1.0f;

        /// <summary>
        ///     The solution inside of this stomach this transfers reagents to the body.
        /// </summary>
        [DataField]
        public Entity<SolutionComponent>? Solution = null;

        /// <summary>
        ///     What solution should this stomach push reagents into, on the body?
        /// </summary>
        [DataField]
        public string BodySolutionName = BloodstreamComponent.DefaultChemicalsSolutionName;

        /// <summary>
        ///     Time in seconds between reagents being ingested and them being
        ///     transferred to <see cref="BloodstreamComponent"/>
        /// </summary>
        [DataField]
        public float DigestionDelay = 20;

        /// <summary>
        ///     A whitelist for what special-digestible-required foods this stomach is capable of eating.
        /// </summary>
        [DataField]
        public EntityWhitelist? SpecialDigestible = null;

        /// <summary>
        ///     Used to track how long each reagent has been in the stomach
        /// </summary>
        [ViewVariables]
        public readonly List<ReagentDelta> ReagentDeltas = new();

        /// <summary>
        ///     Used to track quantity changes when ingesting & digesting reagents
        /// </summary>
        public sealed class ReagentDelta
        {
            public readonly ReagentQuantity ReagentQuantity;
            public float Lifetime { get; private set; }

            public ReagentDelta(ReagentQuantity reagentQuantity)
            {
                ReagentQuantity = reagentQuantity;
                Lifetime = 0.0f;
            }

            public void Increment(float delta) => Lifetime += delta;
        }
    }
}
