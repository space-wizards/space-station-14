namespace Content.Server.Delete
{
    /// <summary>
    /// Deletes the entity after the specified period of time.
    /// </summary>
    [RegisterComponent]
    public sealed class DeleteAfterTimeComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("accumulator")]
        public float Accumulator = 0f;

        [ViewVariables(VVAccess.ReadWrite), DataField("despawnTime")]
        public TimeSpan DespawnTime = TimeSpan.FromSeconds(30);
    }
}
