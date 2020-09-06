using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AtmosExposedSystem
    : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

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
                var tile = atmosExposedComponent.Owner.Transform.Coordinates.GetTileAtmosphere(_entityManager);
                if (tile == null) continue;
                atmosExposedComponent.Update(tile, _lastUpdate);
            }

            _lastUpdate = 0;
        }
    }
}
