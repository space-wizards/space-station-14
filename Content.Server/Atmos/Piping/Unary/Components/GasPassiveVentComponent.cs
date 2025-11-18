namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed partial class GasPassiveVentComponent : Component
    {
        [DataField("inlet")]
        public string InletName = "pipe";
    }
}
