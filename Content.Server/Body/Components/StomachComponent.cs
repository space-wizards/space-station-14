using Content.Server.Body.Systems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Access(typeof(StomachSystem), typeof(FoodSystem))]
    public sealed class StomachComponent : Component
    {
        public float AccumulatedFrameTime;

        /// <summary>
        ///     How fast should this component update, in seconds?
        /// </summary>
        [DataField("updateInterval")]
        public float UpdateInterval = 1.0f;

        /// <summary>
        ///     What solution should this stomach push reagents into, on the body?
        /// </summary>
        [DataField("bodySolutionName")]
        public string BodySolutionName = BloodstreamComponent.DefaultChemicalsSolutionName;

        /// <summary>
        ///     Time in seconds between reagents being ingested and them being
        ///     transferred to <see cref="BloodstreamComponent"/>
        /// </summary>
        [DataField("digestionDelay")]
        public float DigestionDelay = 20;

        /// <summary>
        ///     A whitelist for what special-digestible-required foods this stomach is capable of eating.
        /// </summary>
        [DataField("specialDigestible")]
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
