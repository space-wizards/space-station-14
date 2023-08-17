namespace Content.Shared.Labels.Components
{
    [RegisterComponent]
    public sealed class LabelComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("currentLabel")]
        public string? CurrentLabel { get; set; }
    }
}
