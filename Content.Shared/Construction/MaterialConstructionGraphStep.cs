using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Materials;
using Content.Shared.Materials;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public class MaterialConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        public StackType Material { get; private set; }
        public int Amount { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Material, "material", StackType.Metal);
            serializer.DataField(this, x => x.Amount, "amount", 1);
        }
    }
}
