namespace Content.Server.Bed.Components
{
    [RegisterComponent]
    public sealed class StasisBedComponent : Component
    {
        /// <summary>
        /// What the metabolic update rate will be multiplied by (higher = slower metabolism)
        /// </summary>
        [DataField("multiplier", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Multiplier = 10f;

        /// <summary>
        /// How much will the power load increase when someone buckles in?
        /// </summary>
        [DataField("addLoadOnBuckle")] public readonly float AddLoadOnBuckle = 1500f;
    }
}
