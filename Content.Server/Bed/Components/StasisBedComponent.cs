namespace Content.Server.Bed.Components
{
    [RegisterComponent]
    public sealed class StasisBedComponent : Component
    {

        // What the metabolic update rate will be multiplied by (higher = slower metabolism)
        [DataField("multiplier", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Multiplier = 10f;
    }
}
