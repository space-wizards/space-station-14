using System.Linq;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed class PrototypeConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("prototype")]
        private string _prototype = "BaseItem";

        [DataField("allowParents")]
        private bool _allowParents;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            entityManager.TryGetComponent(uid, out MetaDataComponent? metaDataComponent);
            if (metaDataComponent?.EntityPrototype == null)
                return false;

            if (metaDataComponent.EntityPrototype.ID == _prototype)
                return true;
            if (_allowParents && metaDataComponent.EntityPrototype.Parents != null)
                return metaDataComponent.EntityPrototype.Parents.Contains(_prototype);
            return false;
        }
    }
}
