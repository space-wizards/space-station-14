using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Botany
{
    class PlantComponent : Component, IAttackBy, IAttackHand, IOnDamageBehavior
    {
        public override string Name => "PlantComponent";

        [ViewVariables(VVAccess.ReadWrite)]
        public float TimeSinceLastUpdate = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public PlantHolderComponent Holder;
        public PlantDNA DNA => Owner.GetComponent<PlantDNAComponent>().DNA;

        [ViewVariables(VVAccess.ReadWrite)]
        private PlantEffects _effects;
        public PlantEffects Effects => _effects;

        [ViewVariables(VVAccess.ReadOnly)]
        public DamageThreshold DeathThreshold { get; private set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public DamageThreshold DestructionThreshold { get; private set; }


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
                if (Effects.dead)
                {
                    sprite.LayerSetSprite(0, DNA.Lifecycle.DeathSprite);
                }
                else
                {
                    sprite.LayerSetSprite(0, CurrentStage().Sprite);
                }
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref TimeSinceLastUpdate, "timeSinceLastUpdate", 0);
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
            if (stage.Harvest != null && stage.Harvest.HarvestPrototype != null)
            {
                var harvestPrototype = stage.Harvest.HarvestPrototype;
                var entityManager = IoCManager.Resolve<IEntityManager>();

                var totalYield = (DNA.YieldMultiplier - 1) + (Effects.YieldMultiplier - 1) + 1;
                var rand = new Random();
                for (int i = 0; i <= totalYield; i++)
                {
                    entityManager.TrySpawnEntityAt(harvestPrototype,
                        Owner.Transform.GridPosition.Offset(new Vector2((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f)),
                        out var harvest);
                    if (harvest.TryGetComponent<PlantDNAComponent>(out var dna))
                    {
                        dna.DNA = (PlantDNA)Owner.GetComponent<PlantDNAComponent>().DNA.Clone();
                    }
                    else
                    {
                        harvest.AddComponent<PlantDNAComponent>().DNA = (PlantDNA)Owner.GetComponent<PlantDNAComponent>().DNA.Clone();
                    }
                }
                Owner.Delete();
                return true;
            }
            return false;
        }

        public List<DamageThreshold> GetAllDamageThresholds()
        {
            DeathThreshold = new DamageThreshold(Shared.GameObjects.DamageType.Total, 100, ThresholdType.Death);
            DestructionThreshold = new DamageThreshold(Shared.GameObjects.DamageType.Total, 150, ThresholdType.Destruction);
            return new List<DamageThreshold>() { DeathThreshold, DestructionThreshold };
        }

        public void OnDamageThresholdPassed(object obj, DamageThresholdPassedEventArgs e)
        {
            if (e.Passed && e.DamageThreshold == DeathThreshold)
            {
                Effects.dead = true;
                UpdateSprite();
            }
            if (e.Passed && e.DamageThreshold == DestructionThreshold)
            {
                Owner.Delete();
            }
        }
    }
}
