using Content.Server.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AtmosExposedSystem
    : EntitySystem
    {
        private const float UpdateDelay = 3f;
        private float _lastUpdate;
        public override void Update(float frameTime)
        {
            _lastUpdate += frameTime;
            if (_lastUpdate < UpdateDelay) return;
            var atmoSystem = EntitySystemManager.GetEntitySystem<AtmosphereSystem>();
            // creadth: everything exposable by atmo should be updated as well
            foreach (var atmosExposedComponent in EntityManager.ComponentManager.EntityQuery<AtmosExposedComponent>())
            {
                var ownerTransform = atmosExposedComponent.Owner.Transform;
                var atmo = atmoSystem.GetGridAtmosphere(ownerTransform.GridID);
                var tile = atmo?.GetTile(ownerTransform.GridPosition);
                if (tile == null) continue;
                atmosExposedComponent.Update(tile, _lastUpdate);
            }

            _lastUpdate = 0;
        }
    }
}
