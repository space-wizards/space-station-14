namespace Content.Server.Access.Components
{
    [RegisterComponent]
    public sealed partial class PresetIdCardComponent : Component
    {
        [DataField("job")]
        public string? JobName { get; private set; }
    }
}
