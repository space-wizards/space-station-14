namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed class GasPassiveVentComponent : Component
    {
        [DataField("inlet")]
        public string InletName = "pipe";
    }
}
