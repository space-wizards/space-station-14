namespace Content.Shared.Eye.Blinding.EyeProtection
{
    /// <summary>
    /// For tools like welders that will damage your eyes when you use them.
    /// </summary>
    [RegisterComponent]
    public sealed class RequiresEyeProtectionComponent : Component
    {
        /// <summary>
        /// How long to apply temporary blindness to the user.
        /// </summary>
        [DataField("statusEffectTime")]
        public TimeSpan StatusEffectTime = TimeSpan.FromSeconds(10);

        /// <summary>
        /// You probably want to turn this on in yaml if it's something always on and not a welder.
        /// </summary>
        [DataField("toggled")]
        public bool Toggled = false;
    }
}
