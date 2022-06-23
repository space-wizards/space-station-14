namespace Content.Server.Abilities.Boxer
{
    /// <summary>
    /// Makes the user begin boxing.
    /// </summary>
    [RegisterComponent]
    public sealed class BoxingGlovesComponent : Component
    {
        /// <summary>
        /// Is the component currently being worn and affecting someone?
        /// Making the unequip check not totally CBT
        /// </summary>
        public bool IsActive = false;
    }
}
