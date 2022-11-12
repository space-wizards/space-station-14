namespace Content.Server.Atmos.Miasma
{
    /// <summary>
    /// Tracking component for stuff that has started to rot.
    /// </summary>
    [RegisterComponent]
    public sealed class RottingComponent : Component
    {
        /// <summary>
        /// Whether or not the rotting should deal damage
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool DealDamage = true;
    }
}
