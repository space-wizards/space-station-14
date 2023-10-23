namespace Content.Server.Access.Components
{
    [RegisterComponent]
    public sealed partial class PresetIdCardComponent : Component
    {
        [DataField("job")]
        public string? JobName;

        [DataField("name")]
        public string? IdName;
    }
}
