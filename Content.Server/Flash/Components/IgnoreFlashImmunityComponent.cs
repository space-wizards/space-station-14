namespace Content.Server.Flash.Components
{
    /// <summary>
    /// Any kind of eye protection will not work on this entity
    /// </summary>
    [RegisterComponent]
    internal sealed class IgnoreFlashImmunityComponent : Component
    {
        /// <summary>
        /// How much the flash should be reduced by for each piece of immunity?
        /// </summary>
        [DataField("FlashMultiplier")]
        public float FlashMultiplier = 0.5f;
    }
}
