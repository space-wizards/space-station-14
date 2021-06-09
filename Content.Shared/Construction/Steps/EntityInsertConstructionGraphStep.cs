#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class EntityInsertConstructionGraphStep : ConstructionGraphStep
    {
        [DataField("store")] public string Store { get; } = string.Empty;

        public abstract bool EntityValid(IEntity entity);
    }
}
