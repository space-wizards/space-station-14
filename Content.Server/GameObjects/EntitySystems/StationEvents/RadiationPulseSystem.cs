using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.Components.Damage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Server.Interfaces.GameObjects.Components;
using Content.Shared.Damage;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.EntitySystems.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        private const string RadiationPrototype = "RadiationPulse";

        public IEntity RadiationPulse(GridCoordinates coordinates, float range, int dps, bool decay = true, float minPulseLifespan = 0.8f, float maxPulseLifespan = 2.5f, string sound = null)
        {
            var radiationEntity = EntityManager.SpawnEntity(RadiationPrototype, coordinates);
            var radiation = radiationEntity.GetComponent<RadiationPulseComponent>();

            radiation.Range = range;
            radiation.DPS = dps;
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

                foreach (var entity in EntityManager.GetEntitiesInRange(ent.Transform.GridPosition, comp.Range, true))
                {
                    foreach (var radiation in entity.GetAllComponents<IRadiationAct>())
                    {
                        radiation.RadiationAct(frameTime, comp);
                    }
                }
            }
        }
    }
}
