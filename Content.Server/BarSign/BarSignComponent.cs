namespace Content.Server.BarSign
{
    [RegisterComponent]
    public sealed class BarSignComponent : Component
    {
        [DataField("current")]
        [ViewVariables(VVAccess.ReadOnly)]
        public string? CurrentSign;
    }
}
