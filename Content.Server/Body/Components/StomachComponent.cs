using Content.Server.Body.Systems;
using Content.Shared.FixedPoint;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Access(typeof(StomachSystem))]
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
        ///     Initial internal solution storage volume
        /// </summary>
        [DataField("initialMaxVolume", readOnly: true)]
        public readonly FixedPoint2 InitialMaxVolume = FixedPoint2.New(50);

        /// <summary>
        ///     Time in seconds between reagents being ingested and them being
        ///     transferred to <see cref="BloodstreamComponent"/>
        /// </summary>
        [DataField("digestionDelay")]
        public float DigestionDelay = 20;

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
