namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed class GasPortableComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("port")]
        public string PortName { get; set; } = "port";

        /// <summary>
        /// Whether we should block anchoring if not connecting to a port.
        /// </summary>
        [DataField("blockAnchor")]
        public bool BlockAnchor = true;
    }
}
