using Content.Shared.Radiation;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Radiation
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        private const string RadiationPrototype = "RadiationPulse";

        public IEntity RadiationPulse(
            EntityCoordinates coordinates,
            float range,
            int dps,
            bool decay = true,
            float minPulseLifespan = 0.8f,
            float maxPulseLifespan = 2.5f,
            string? sound = null)
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

            var lookupSystem = IoCManager.Resolve<IEntityLookup>();

            foreach (var comp in ComponentManager.EntityQuery<RadiationPulseComponent>(true))
            {
                comp.Update(frameTime);
                var ent = comp.Owner;

                if (ent.Deleted) continue;

                foreach (var entity in lookupSystem.GetEntitiesInRange(ent.Transform.Coordinates, comp.Range, true))
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
