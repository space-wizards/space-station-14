using Content.Server.GameObjects.Components.StationEvents;
using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        private const string RadiationPrototype = "RadiationPulse";

        public IEntity RadiationPulse(EntityCoordinates coordinates, float range, int dps, bool decay = true, float minPulseLifespan = 0.8f, float maxPulseLifespan = 2.5f, string sound = null)
        {
            var radiationEntity = EntityManager.SpawnEntity(RadiationPrototype, coordinates);
            var radiation = radiationEntity.GetComponent<RadiationPulseComponent>();

            radiation.Range = range;
            radiation.RadsPerSecond = dps;
            radiation.Draw = false;
            radiation.Decay = decay;
            radiation.MinPulseLifespan = minPulseLifespan;
            radiation.MaxPulseLifespan = maxPulseLifespan;
            radiation.Sound = sound;

            radiation.DoPulse();

            return radiationEntity;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in ComponentManager.EntityQuery<RadiationPulseComponent>())
            {
                comp.Update(frameTime);
                var ent = comp.Owner;

                if (ent.Deleted) continue;

                foreach (var entity in EntityManager.GetEntitiesInRange(ent.Transform.Coordinates, comp.Range, true))
                {
                    if (entity.Deleted) continue;

                    foreach (var radiation in entity.GetAllComponents<IRadiationAct>())
                    {
                        radiation.RadiationAct(frameTime, comp);
                    }
                }
            }
        }
    }
}
