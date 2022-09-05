namespace Content.Server.Lathe.Components
{
    /// <summary>
    /// For EntityQuery to keep track of which lathes are inserting
    /// </summary>
    [RegisterComponent]
    public sealed class LatheInsertingComponent : Component
    {
        /// <summary>
        /// Remaining insertion time, in seconds.
        /// </summary>
        [DataField("timeRemaining", required: true)]
        public float TimeRemaining;
    }
}
