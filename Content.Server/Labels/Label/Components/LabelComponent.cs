namespace Content.Server.Labels.Components
{
    /// <summary>
    /// Makes entities have a label in their name. Labels are normally given by <see cref="HandLabelerComponent"/>
    /// </summary>
    [RegisterComponent]
    public sealed partial class LabelComponent : Component
    {
        /// <summary>
        /// The actual text on the label
        /// Entity Prototypes pre-configured with a label will resolve a localization string entered here, when the entity spawns.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("currentLabel")]
        public string? CurrentLabel { get; set; }

        /// <summary>
        ///  The original name of the entity
        ///  Used for reverting the modified entity name when the label is removed
        /// </summary>
        [DataField("originalName")]
        public string? OriginalName { get; set; }
    }
}
