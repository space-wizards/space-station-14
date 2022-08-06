namespace Content.Server.Atmos.Piping.Components
{
    [RegisterComponent]
    public sealed class AtmosUnsafeUnanchorComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;
    }
}
