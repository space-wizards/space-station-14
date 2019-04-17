using Content.Shared.GameObjects.Components.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Research
{
    public class LatheMaterialComponent : Component
    {
        private LatheMaterial _material = LatheMaterial.Metal;
        private uint _quantity = 0u;

        public override string Name => "LatheMaterial";
        public LatheMaterial Material {
            get => _material;
            private set => _material = value;
        }
        public uint Quantity
        {
            get => _quantity;
            private set => _quantity = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _material, "Material", LatheMaterial.Metal);
            serializer.DataField(ref _quantity, "Quantity", 0u);
        }
    }
}
