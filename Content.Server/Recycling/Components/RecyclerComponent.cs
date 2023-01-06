using Content.Shared.Recycling;

namespace Content.Server.Recycling
{
    // TODO: Add sound and safe beep
    [RegisterComponent]
    [ComponentReference(typeof(SharedRecyclerComponent))]
    public sealed class RecyclerComponent : SharedRecyclerComponent
    {
        [DataField("enabled")]
        public bool Enabled;

        /// <summary>
        ///     Whether or not sentient beings will be recycled
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("safe")]
        public bool Safe = true;

        /// <summary>
        ///     The percentage of material that will be recovered
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("efficiency")]
        public float Efficiency = 0.25f;
    }
}

