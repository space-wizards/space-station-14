namespace Content.Server.Atmos.Miasma
{
    [RegisterComponent]
    /// <summary>
    /// Tracking component for stuff that has started to rot.
    /// </summary>
    public sealed class RottingComponent : Component
    {
        /// <summary>
        /// Whether or not the rotting should deal damage
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool DealDamage = true;
    }
}
