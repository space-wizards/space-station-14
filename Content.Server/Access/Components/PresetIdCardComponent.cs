namespace Content.Server.Access.Components
{
    [RegisterComponent]
    public sealed class PresetIdCardComponent : Component
    {
        [DataField("job")]
        public readonly string? JobName;
    }
}
