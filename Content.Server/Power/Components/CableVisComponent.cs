namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed class CableVisComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("node")]
        public string? Node;
    }
}
