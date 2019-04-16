using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

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
        /// <param name="grid"></param>
        /// <param name="localPosition"></param>
        public static void SpawnLightTurret(IMapGrid grid, Vector2 localPosition)
        {
            var entMan = IoCManager.Resolve<IServerEntityManager>();
            var tBase = entMan.SpawnEntity("TurretBase");
            tBase.GetComponent<ITransformComponent>().GridPosition = new GridCoordinates(localPosition, grid);

            var tTop = entMan.SpawnEntity("TurretTopLight");
            var topTransform = tTop.GetComponent<ITransformComponent>();
            topTransform.GridPosition = new GridCoordinates(localPosition, grid);
            topTransform.AttachParent(tBase);
        }
    }
}
