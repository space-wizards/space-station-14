namespace Content.Server.Bed.Components
{
    [RegisterComponent]
    public sealed class StasisBedComponent : Component
    {
        [DataField("multiplier", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Multiplier = 10f;
    }
}
