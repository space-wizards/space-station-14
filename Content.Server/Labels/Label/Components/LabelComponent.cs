namespace Content.Server.Labels.Components
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

        [DataField("originalName")]
        public string? OriginalName { get; set; }
    }
}
