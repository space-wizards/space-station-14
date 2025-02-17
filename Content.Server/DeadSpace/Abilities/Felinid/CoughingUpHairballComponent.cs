namespace Content.Server.DeadSpace.Abilities.Felinid
{
    [RegisterComponent]
    public sealed partial class CoughingUpHairballComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("coughUpTime")]
        public TimeSpan CoughUpTime = TimeSpan.FromSeconds(2.15); // length of hairball.ogg
    }
}
