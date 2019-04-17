using System;
using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components.Research
{
    public enum LatheType
    {
        Autolathe,
        Protolathe,
    }

    public enum LatheMaterial
    {
        Metal,
        Glass,
    }

    public class SharedLatheComponent : Component
    {
        protected Dictionary<LatheMaterial, uint> _materialStorage;
        private List<LatheMaterial> _acceptedMaterials = new List<LatheMaterial>() {LatheMaterial.Metal, LatheMaterial.Glass};
        public override string Name => "Lathe";
        public override uint? NetID => ContentNetIDs.LATHE;
        public LatheType LatheType = LatheType.Autolathe;

        public bool CanProduce(LatheRecipePrototype recipe, uint quantity = 1)
        {

            foreach (var (material, materialQuantity) in recipe.RequiredMaterials)
            {
                if (!HasMaterial(material, materialQuantity * quantity)) return false;
            }
            return true;
        }

        public bool AcceptsMaterial(LatheMaterial material)
        {
            return _acceptedMaterials.Contains(material);
        }

        public bool HasMaterial(LatheMaterial material, uint quantity)
        {
            if (!AcceptsMaterial(material)) return false;
            if (_materialStorage[material] >= quantity) return true;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _acceptedMaterials, "AcceptedMaterials", new List<LatheMaterial>() {LatheMaterial.Metal, LatheMaterial.Glass});
        }

        [Serializable, NetSerializable]
        public class LatheMenuOpenMessage : ComponentMessage
        {
            public LatheMenuOpenMessage()
            {
                Directed = true;
            }
        }

        [Serializable, NetSerializable]
        public class LatheMaterialsUpdateMessage : ComponentMessage
        {
            public readonly Dictionary<LatheMaterial, uint> MaterialStorage;
            public LatheMaterialsUpdateMessage(Dictionary<LatheMaterial, uint> storage)
            {
                Directed = true;
                MaterialStorage = storage;
            }
        }
    }
}
