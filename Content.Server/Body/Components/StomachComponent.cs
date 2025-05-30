using Content.Server.Body.Systems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Access(typeof(StomachSystem), typeof(FoodSystem))]
    public sealed partial class StomachComponent : Component
    {
        /// <summary>
        ///     The next time that the stomach will try to digest its contents.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextUpdate;

        /// <summary>
        ///     The interval at which this stomach digests its contents.
        /// </summary>
        [DataField]
        public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        ///     The solution inside of this stomach this transfers reagents to the body.
        /// </summary>
        [ViewVariables]
        public Entity<SolutionComponent>? Solution;

        /// <summary>
        ///     What solution should this stomach push reagents into, on the body?
        /// </summary>
        [DataField]
        public string BodySolutionName = BloodstreamComponent.DefaultChemicalsSolutionName;

        /// <summary>
        ///     Time between reagents being ingested and them being
        ///     transferred to <see cref="BloodstreamComponent"/>
        /// </summary>
        [DataField]
        public TimeSpan DigestionDelay = TimeSpan.FromSeconds(20);

        /// <summary>
        ///     A whitelist for what special-digestible-required foods this stomach is capable of eating.
        /// </summary>
        [DataField]
        public EntityWhitelist? SpecialDigestible = null;

        /// <summary>
        /// Controls whitelist behavior. If true, this stomach can digest <i>only</i> food that passes the whitelist. If false, it can digest normal food <i>and</i> any food that passes the whitelist.
        /// </summary>
        [DataField]
        public bool IsSpecialDigestibleExclusive = true;

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
            public TimeSpan Lifetime { get; private set; }

            public ReagentDelta(ReagentQuantity reagentQuantity)
            {
                ReagentQuantity = reagentQuantity;
                Lifetime = TimeSpan.Zero;
            }

            public void Increment(TimeSpan delta) => Lifetime += delta;
        }
    }
}
