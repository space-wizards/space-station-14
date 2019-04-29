using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Botany
{
    class PlantComponent : Component
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
    }
}
