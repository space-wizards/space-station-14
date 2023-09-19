namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Component that removes an item when a specific solution in it becomes empty.
    /// </summary>
    [RegisterComponent]
    public sealed partial class DeleteOnEmptyComponent : Component
    {
        /// <summary>
        /// The name of the solution of which to check emptiness
        /// </summary>
        [DataField("solution")]
        public string Solution { get; set; } = string.Empty;
    }
}
