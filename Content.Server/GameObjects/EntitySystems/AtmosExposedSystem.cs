using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AtmosExposedSystem
    : EntitySystem
    {

        private const float UpdateDelay = 1f;
        private float _lastUpdate;

        public override void Update(float frameTime)
        {
            _lastUpdate += frameTime;
            if (_lastUpdate < UpdateDelay) return;

            // creadth: everything exposable by atmos should be updated as well
            foreach (var atmosExposedComponent in EntityManager.ComponentManager.EntityQuery<AtmosExposedComponent>(true))
            {
                var tile = atmosExposedComponent.Owner.Transform.Coordinates.GetTileAtmosphere();
                if (tile == null) continue;
                atmosExposedComponent.Update(tile, _lastUpdate);
            }

            _lastUpdate = 0;
        }
    }
}
