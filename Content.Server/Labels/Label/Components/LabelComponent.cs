namespace Content.Server.Labels.Components
{
    /// <summary>
    /// Gives opportunity to add labels by entities with the <see cref="HandLabelerComponent"/>
    /// </summary>
    [RegisterComponent]
    public sealed class LabelComponent : Component
    {
        /// <summary>
        /// Label text. If you using that field in your prototype, you must set originalName field too, and set name of entity like it with label
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("currentLabel")]
        public string? CurrentLabel { get; set; }

        /// <summary>
        /// Name of entity without label
        /// </summary>
        [DataField("originalName")]
        public string? OriginalName { get; set; }
    }
}
