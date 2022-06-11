namespace Content.Server.Flash.Components
{
    [RegisterComponent]
    public sealed class FlashImmunityComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;
    }
}
