namespace Content.Server.Flash.Components
{
    [RegisterComponent, Friend(typeof(FlashSystem))]
    public sealed class FlashImmunityComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;
    }
}
