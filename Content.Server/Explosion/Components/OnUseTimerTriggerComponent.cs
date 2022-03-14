namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class OnUseTimerTriggerComponent : Component
    {
        [DataField("delay")]
        public float Delay = 1f;

        /// <summary>
        ///     If not null, a user can use verbs to configure the delay to one of these options.
        /// </summary>
        [DataField("delayOptions")]
        public List<float>? DelayOptions = null;

        [DataField("singleUse")]
        public readonly bool SingleUse = true;

        [DataField("used")]
        public bool Used = false;
    }
}
