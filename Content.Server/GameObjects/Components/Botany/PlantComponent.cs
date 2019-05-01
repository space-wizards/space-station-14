using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Botany
{
    class PlantComponent : Component, IAttackBy, IAttackHand
    {
        public override string Name => "PlantComponent";

        [ViewVariables(VVAccess.ReadWrite)]
        public float TimeSinceLastUpdate = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public PlantHolderComponent Holder;

        [ViewVariables(VVAccess.ReadWrite)]
        private PlantDNA _dna;
        public PlantDNA DNA { //useless property rn
            get { return _dna; }
            set { _dna = value; }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        private PlantEffects _effects;
        public PlantEffects Effects => _effects;

        private PlantStage CurrentStage()
        {
            return DNA.Lifecycle.LifecycleNodes.Single(node => node.NodeID == Effects.currentLifecycleNodeID);
        }

        public void UpdateCurrentStage()
        {
            PlantStage minimalStage = null;
            foreach (var stage in DNA.Lifecycle.LifecycleNodes)
            {
                if (minimalStage == null ||
                    (stage.lifeProgressRequiredInSeconds < Effects.lifeProgressInSeconds && stage.lifeProgressRequiredInSeconds > minimalStage.lifeProgressRequiredInSeconds))
                {
                    minimalStage = stage;
                }
            }
            if (minimalStage.NodeID != Effects.currentLifecycleNodeID)
            {
                Effects.currentLifecycleNodeID = minimalStage.NodeID;
                UpdateSprite();
            }
        }

        public void UpdateSprite()
        {
            if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
            {
                sprite.LayerSetSprite(0, CurrentStage().Sprite);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref TimeSinceLastUpdate, "timeSinceLastUpdate", 0);
            serializer.DataField(ref _dna, "dna", new PlantDNA());
            serializer.DataField(ref _effects, "effects", new PlantEffects());
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Holder.HeldPlant = null;
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            //using tools to create cuttings, harvest, heal, etc
            return false;
        }

        public bool AttackHand(AttackHandEventArgs eventArgs)
        {
            var stage = CurrentStage();
            var harvestPrototype = stage.HarvestPrototype;
            if (harvestPrototype != null)
            {
                var entityManager = IoCManager.Resolve<IEntityManager>();

                //todo: add DNA to the harvested entity
                var totalYield = (DNA.YieldMultiplier - 1) + (Effects.YieldMultiplier - 1) + 1;
                var rand = new Random();
                for (int i = 0; i <= totalYield; i++)
                {
                    entityManager.TrySpawnEntityAt(harvestPrototype,
                        Owner.Transform.GridPosition.Offset(new Vector2((float)rand.NextDouble() -0.5f, (float)rand.NextDouble() -0.5f)),
                        out var harvested);
                    if (!harvested.HasComponent<PlantSeedComponent>() && stage.HarvestSeedPrototype != null)
                    {
                        var seedContainer = harvested.AddComponent<PlantSeedContainerComponent>();
                        seedContainer.seedPrototype = stage.HarvestSeedPrototype;
                        seedContainer.DNA = (PlantDNA)DNA.Clone();
                    }
                }
                Owner.Delete();
                return true;
            }
            return false;
        }
    }
}
