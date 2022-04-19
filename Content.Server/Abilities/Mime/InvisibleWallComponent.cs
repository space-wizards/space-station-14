namespace Content.Server.Abilities.Mime
{
    // Tracks invisible wall despawning
    [RegisterComponent]
    public sealed class InvisibleWallComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        public float DespawnTime = 30f;
    }
}
