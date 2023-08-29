namespace Content.Server.Nutrition.Components
{
    /// <summary>
    /// Component that tags solution containers as trash when their contents have been emptied.
    /// Used for things like used ketchup packets or used syringes.
    /// </summary>
    [RegisterComponent]
    public sealed partial class TrashOnEmptyComponent : Component
    {
        /// <summary>
        /// The name of the solution of which to check emptiness
        /// </summary>
        [DataField("solution")]
        public string Solution { get; set; } = string.Empty;
    }
}
