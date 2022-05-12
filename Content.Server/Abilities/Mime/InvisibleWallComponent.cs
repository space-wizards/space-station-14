namespace Content.Server.Abilities.Mime
{
    // Tracks invisible wall despawning
    [RegisterComponent]
    public sealed class InvisibleWallComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;
        [DataField("despawnTime")]
        public TimeSpan DespawnTime = TimeSpan.FromSeconds(30);
    }
}
