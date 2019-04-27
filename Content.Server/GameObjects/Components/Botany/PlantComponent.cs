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
        public PlantDNA DNA => _dna;

        [ViewVariables(VVAccess.ReadWrite)]
        private PlantEffects _effects;
        public PlantEffects Effects;

        public void UpdateSprite()
        {
            if (!Owner.TryGetComponent<SpriteComponent>(out var sprite))
            {
                return;
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
