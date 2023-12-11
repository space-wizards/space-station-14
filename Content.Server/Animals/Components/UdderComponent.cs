using Content.Server.Animals.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;


namespace Content.Server.Animals.Components

/// <summary>
///     Lets an entity produce milk. Uses hunger if present.
/// </summary>
{
    [RegisterComponent, Access(typeof(UdderSystem))]
    internal sealed partial class UdderComponent : Component
    {
        /// <summary>
        ///     The reagent to produce.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public ProtoId<ReagentPrototype> ReagentId = "Milk";

        /// <summary>
        ///     The solution to add reagent to.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public string Solution = "udder";

        /// <summary>
        ///     The amount of reagent to be generated on update.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public FixedPoint2 QuantityPerUpdate = 25;

        /// <summary>
        ///     The amount of nutrient consumed on update.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float HungerUsage = 10f;

        /// <summary>
        ///     How long to wait before producing.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan GrowthDelay = TimeSpan.FromMinutes(1);

        /// <summary>
        ///     When to next try to produce.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan NextGrowth = TimeSpan.FromSeconds(0);
    }
}
