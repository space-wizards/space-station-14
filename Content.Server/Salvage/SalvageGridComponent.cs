namespace Content.Server.Salvage
{
    /// <summary>
    /// A grid spawned by a salvage magnet.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SalvageGridComponent : Component
    {
        /// <summary>
        /// The magnet that spawned this grid.
        /// </summary>
        public EntityUid? SpawnerMagnet;
    }
}
