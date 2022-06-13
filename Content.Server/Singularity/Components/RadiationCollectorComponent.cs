using Content.Server.Singularity.EntitySystems;

namespace Content.Server.Singularity.Components
{
    /// <summary>
    ///     Generates electricity from radiation.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(RadiationCollectorSystem))]
    public sealed class RadiationCollectorComponent : Component
    {
        /// <summary>
        ///     How much joules will collector generate for each rad.
        /// </summary>
        [DataField("chargeModifier")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ChargeModifier = 30000f;

        /// <summary>
        ///     Cooldown time between users interaction.
        /// </summary>
        [DataField("cooldown")]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan Cooldown = TimeSpan.FromSeconds(0.81f);

        /// <summary>
        ///     Was machine activated by user?
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public bool Enabled;

        /// <summary>
        ///     Timestamp when machine can be deactivated again.
        /// </summary>
        public TimeSpan CoolDownEnd;
    }
}
