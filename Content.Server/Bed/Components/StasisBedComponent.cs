namespace Content.Server.Bed.Components
{
    [RegisterComponent]
    public sealed partial class StasisBedComponent : Component
    {
        /// <summary>
        /// What the metabolic update rate will be multiplied by (higher = slower metabolism)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Multiplier = 10f;
    }
}
