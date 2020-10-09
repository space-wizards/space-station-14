using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Placement
{
    /// <summary>
    ///     Helper function for spawning more complex multi-entity structures
    /// </summary>
    public static class SpawnHelpers
    {
        /// <summary>
        ///     Spawns a spotlight ground turret that will track any living entities in range.
        /// </summary>
        /// <param name="position"></param>
        public static void SpawnLightTurret(EntityCoordinates position)
        {
            var entMan = IoCManager.Resolve<IServerEntityManager>();
            var tBase = entMan.SpawnEntity("TurretBase", position);

            var tTop = entMan.SpawnEntity("TurretTopLight", position);
            tTop.Transform.AttachParent(tBase);
        }
    }
}
