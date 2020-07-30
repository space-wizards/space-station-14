namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     Component interface that gets triggered after the values of an <see cref="BaseDamageableComponent"/> on the same
    ///     IEntity change.
    /// </summary>
    internal interface IOnHealthChangedBehavior
    {
        /// <summary>
        ///     Called when the entity's <see cref="BaseDamageableComponent"/> is healed or hurt. Of note is that a "deal 0 damage"
        ///     call will still trigger
        ///     this function (including both damage negated by resistance or simply inputting 0 as the amount of damage to deal).
        /// </summary>
        /// <param name="e">Details of how the health changed.</param>
        public void OnHealthChanged(HealthChangedEventArgs e);
    }
}
