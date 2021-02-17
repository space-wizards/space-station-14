using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction
{
    [DataDefinition]
    public abstract class EntityInsertConstructionGraphStep : ConstructionGraphStep
    {
        [DataField("store")] public string Store { get; private set; } = string.Empty;

        public abstract bool EntityValid(IEntity entity);
    }
}
