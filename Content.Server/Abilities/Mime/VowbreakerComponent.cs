namespace Content.Server.Abilities.Mime
{
    [RegisterComponent]
    public sealed class VowbreakerComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("vowCooldown")]
        public float VowCooldown = 300f;
    }
}
