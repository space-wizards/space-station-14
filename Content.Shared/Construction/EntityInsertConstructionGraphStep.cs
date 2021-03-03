#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public abstract class EntityInsertConstructionGraphStep : ConstructionGraphStep
    {
        public string Store { get; private set; } = string.Empty;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Store, "store", string.Empty);
        }

        public abstract bool EntityValid(IEntity entity);
    }
}
