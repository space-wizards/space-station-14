namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class TriggerRandomlyComponent : Component
    {
		[DataField("chance")]
        public float TriggerChance = 1.0f;
        [DataField("timerTime", required: false)]
        public float TimerTime = 0.0f;
        public float Accumulator = 0.0f;
        public float UpdateCooldown = 5.0f;
    }
}
