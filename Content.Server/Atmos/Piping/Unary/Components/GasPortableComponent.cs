namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed class GasPortableComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("port")]
        public string PortName { get; set; } = "port";
    }
}
