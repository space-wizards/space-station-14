using Content.Server.GameObjects.Components.Botany;
using Content.Shared.Maths;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.EntitySystems
{
    class BotanySystem : EntitySystem
    {
        private PlantUpdates plantUpdates;

        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(PlantComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in RelevantEntities)
            {
                var plantComponent = entity.GetComponent<PlantComponent>();

                plantComponent.TimeSinceLastUpdate += frameTime;

                if (plantComponent.TimeSinceLastUpdate < 2)
                {
                    continue;
                }
                plantUpdates = new PlantUpdates(entity);
                ProcessSubstrate();
                ProcessLighting();
                ProcessTemperature();
                // process ... [light, food, water, pests, etc] implement these as the state of the game advances
                ApplyAging();
                ApplyGrowth();
                ApplyDamage();
                plantComponent.CheckBasicTransitions();
                plantComponent.TimeSinceLastUpdate = 0;
            }
        }

        private void ProcessSubstrate()
        {
            if (plantUpdates.PlantComponent.Holder == null)
            {
                LimitLifeProgressDelta(0.0);
                return;
            }
            else
            {
                switch (plantUpdates.PlantComponent.Holder.HeldSubstrate)
                {
                    case Substrate.Empty:
                        LimitLifeProgressDelta(0.1);
                        break;
                    case Substrate.Rockwool:
                        LimitLifeProgressDelta(0.5);
                        break;
                    case Substrate.Sand:
                        LimitLifeProgressDelta(1.0);
                        break;
                }
            }
        }

        /// <summary>
        /// crappy off-brand lighting calculations because I can't be bothered to implement measurement of light at a point
        /// </summary>
        private void ProcessLighting()
        {
            var plantEntity = plantUpdates.PlantEntity;
            var pred = new TypeEntityQuery(typeof(PointLightComponent));
            var lights = EntityManager.GetEntities(pred);
            double lightQuantity = 0.0;
            foreach (var lightEntity in lights)
            {
                var distance = lightEntity.Transform.GridPosition.Distance(IoCManager.Resolve<IMapManager>(), plantEntity.Transform.GridPosition);
                var light = lightEntity.GetComponent<PointLightComponent>();
                if (light.Running == false)
                {
                    continue;
                }
                if (distance <= light.Radius)
                {
                    var plantPosition = plantEntity.Transform.MapPosition.Position;
                    var lightPosition = lightEntity.Transform.MapPosition.Position;
                    var ray = new Ray(plantPosition, lightPosition - plantPosition, 1);
                    var castResults = IoCManager.Resolve<IPhysicsManager>().IntersectRay(ray, distance, plantEntity);
                    if (castResults.DidHitObject && castResults.HitEntity != lightEntity)
                    {
                        //for debugging:
                        //IoCManager.Resolve<ILogManager>().GetSawmill("Botany").Log(Robust.Shared.Log.LogLevel.Debug, "distance: " + distance + ", radius: " + light.Radius + "light: " + lightEntity + ", hit: " + castResults.HitEntity);
                        continue;
                    }
                }
                if (distance < 5)
                {
                    lightQuantity = 1.0;
                }
            }
            if (lightQuantity == 1.0)
            {
                LimitLifeProgressDelta(1.0);
            }
            else
            {
                LimitLifeProgressDelta(0.5);
            }
        }

        private void ProcessTemperature()
        {
            var tempComponent = plantUpdates.PlantEntity.GetComponent<TemperatureComponent>();
            if (tempComponent.CurrentTemperature > PhysicalConstants.ZERO_CELCIUS + 50)
            {
                LimitLifeProgressDelta(0.3);
            }
            else if (tempComponent.CurrentTemperature < PhysicalConstants.ZERO_CELCIUS + 5)
            {
                // slow down cellular age?
                LimitLifeProgressDelta(0.3);
            }
            return;
        }

        private void ApplyAging()
        {
            plantUpdates.PlantComponent.cellularAgeInSeconds += plantUpdates.PlantComponent.TimeSinceLastUpdate;
        }

        private void ApplyDamage()
        {
            foreach (var damageDelta in plantUpdates.PlantComponent.damageDeltas)
            {
                var timeSinceLastUpdate = plantUpdates.PlantComponent.TimeSinceLastUpdate;
                var dps = damageDelta.amountPerSecond;
                var dmgType = damageDelta.type;
                if (dmgType == Shared.GameObjects.DamageType.Toxic && plantUpdates.PlantComponent.dead)
                {
                    // snowflake code to not make dead plants disappear super fast
                    dps = 1;
                }
                var damage = Math.Max(1, (int)(dps * timeSinceLastUpdate));
                plantUpdates.PlantDamage.TakeDamage(dmgType, damage);
            }
        }

        private void ApplyGrowth()
        {
            if (!plantUpdates.PlantComponent.dead)
            {
                var lifeProgressDelta = plantUpdates.PlantComponent.TimeSinceLastUpdate * plantUpdates.baseLifeProgressDelta;
                plantUpdates.PlantComponent.progressInSeconds += lifeProgressDelta;
            }
        }

        private void LimitLifeProgressDelta(double maxProgressThisCycle)
        {
            plantUpdates.baseLifeProgressDelta = Math.Min(maxProgressThisCycle, plantUpdates.baseLifeProgressDelta);
        }
    }

    /// <summary>
    /// Holds plant update related variables for Update loop
    /// </summary>
    class PlantUpdates
    {
        public IEntity PlantEntity;

        public PlantComponent PlantComponent;
        public PlantDNAComponent PlantDNA;
        public DamageableComponent PlantDamage;

        public Random Rand = new Random();

        public double baseLifeProgressDelta = 1.0;

        public PlantUpdates(IEntity plantEntity)
        {
            PlantEntity = plantEntity;
            PlantComponent = plantEntity.GetComponent<PlantComponent>();
            PlantDNA = plantEntity.GetComponent<PlantDNAComponent>();
            PlantDamage = PlantEntity.GetComponent<DamageableComponent>();
        }
    }
}
