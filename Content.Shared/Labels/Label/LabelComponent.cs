namespace Content.Shared.Labels.Components
{
    /// <summary>
    /// Makes entities have a label in their name. Labels are normally given by <see cref="HandLabelerComponent"/>
    /// </summary>
    [RegisterComponent]
    public sealed partial class LabelComponent : Component
    {
        /// <summary>
        /// The actual text in the label
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("currentLabel")]
        public string? CurrentLabel { get; set; }

        public string? OriginalName { get; set; }
    }
}
