using Content.Server.GameObjects.Components.Botany;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.EntitySystems
{
    class BotanySystem : EntitySystem
    {
        private PlantUpdateState _plantUpdateState;

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
                _plantUpdateState = new PlantUpdateState(entity);
                ProcessSubstrate();
                // process ... [light, food, water, pests, etc] implement these as the state of the game advances
                ApplyAging();
                ApplyGrowth();
                plantComponent.TimeSinceLastUpdate = 0;
            }
        }

        private void ProcessSubstrate()
        {
            if (_plantUpdateState.PlantComponent.Holder == null)
            {
                LimitLifeProgressDelta(0.0);
                return;
            }
            else
            {
                switch (_plantUpdateState.PlantComponent.Holder.HeldSubstrate)
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

        private void ApplyAging()
        {
            _plantUpdateState.PlantComponent.Effects.cellularAgeInSeconds += _plantUpdateState.PlantComponent.TimeSinceLastUpdate;

            if (_plantUpdateState.PlantComponent.Effects.cellularAgeInSeconds > _plantUpdateState.PlantDNA.DNA.MaxAgeInSeconds)
            {
                _plantUpdateState.PlantDamage.TakeDamage(Shared.GameObjects.DamageType.Toxic, (int)(5 * _plantUpdateState.PlantComponent.TimeSinceLastUpdate));
            }
        }

        private void ApplyGrowth()
        {
            if (!_plantUpdateState.PlantComponent.Effects.dead)
            {
                var lifeProgressDelta = _plantUpdateState.PlantComponent.TimeSinceLastUpdate * _plantUpdateState.baseLifeProgressDelta;
                _plantUpdateState.PlantComponent.Effects.progressInSeconds += lifeProgressDelta;
                _plantUpdateState.PlantComponent.Effects.stageEffects.progressInSeconds += lifeProgressDelta;
                _plantUpdateState.PlantComponent.UpdateCurrentStage();
            }
        }

        private void LimitLifeProgressDelta(double maxProgressThisCycle)
        {
            _plantUpdateState.baseLifeProgressDelta = Math.Min(maxProgressThisCycle, _plantUpdateState.baseLifeProgressDelta);
        }
    }

    /// <summary>
    /// Temporary state management of plant related info in update loop
    /// </summary>
    class PlantUpdateState
    {
        public IEntity PlantEntity;

        public PlantComponent PlantComponent;
        public PlantDNAComponent PlantDNA;
        public DamageableComponent PlantDamage;

        public Random Rand = new Random();

        public double baseLifeProgressDelta = 1.0;

        public PlantUpdateState(IEntity plantEntity)
        {
            PlantEntity = plantEntity;
            PlantComponent = plantEntity.GetComponent<PlantComponent>();
            PlantDNA = plantEntity.GetComponent<PlantDNAComponent>();
            PlantDamage = PlantEntity.GetComponent<DamageableComponent>();
        }
    }
}
