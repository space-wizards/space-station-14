namespace Content.Shared.Eye.Blinding.EyeProtection
{
    /// <summary>
    /// For welding masks, sunglasses, etc.
    /// </summary>
    [RegisterComponent]
    public sealed class EyeProtectionComponent : Component
    {
        /// <summary>
        /// How many seconds to subtract from the status effect. If it's greater than the source
        /// of blindness, do not blind.
        /// </summary>
        [DataField("protectionTime")]
        public TimeSpan ProtectionTime = TimeSpan.FromSeconds(10);

        public bool IsActive = false;
    }
}
