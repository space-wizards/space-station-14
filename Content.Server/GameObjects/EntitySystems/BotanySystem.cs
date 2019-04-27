using Content.Server.GameObjects.Components.Botany;
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

                var plantUpdater = new PlantUpdater(entity);
                plantUpdater.Update();
            }
        }

        private class PlantUpdater
        {
            readonly float _frameTime;

            private readonly IEntity _plantEntity;
            private readonly PlantComponent _plantComponent;
            private readonly Random _rand = new Random();

            internal void Update()
            {
                ProcessSubstrate();
            }

            private void ProcessSubstrate()
            {
                if (_plantComponent.Holder == null)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    switch (_plantComponent.Holder.Substrate)
                    {
                        case SubstrateType.Empty:
                            LimitProgress(0.1);
                            break;
                    }
                }
            }
        }
    }
}
