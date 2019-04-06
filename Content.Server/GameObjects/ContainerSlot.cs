using SS14.Server.GameObjects.Components.Container;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using System.Collections.Generic;
using SS14.Shared.ViewVariables;

namespace Content.Server.GameObjects
{
    public class ContainerSlot : BaseContainer
    {
        [ViewVariables]
        public IEntity ContainedEntity { get; private set; } = null;

        /// <inheritdoc />
        public override IReadOnlyCollection<IEntity> ContainedEntities => new List<IEntity> { ContainedEntity }.AsReadOnly();

        public ContainerSlot(string id, IContainerManager manager) : base(id, manager)
        {
        }

        /// <inheritdoc />
        public override bool CanInsert(IEntity toinsert)
        {
            if (ContainedEntity != null)
                return false;
            return base.CanInsert(toinsert);
        }

        /// <inheritdoc />
        public override bool Contains(IEntity contained)
        {
            if (contained != null && contained == ContainedEntity)
                return true;
            return false;
        }

        /// <inheritdoc />
        protected override void InternalInsert(IEntity toinsert)
        {
            ContainedEntity = toinsert;
        }

        /// <inheritdoc />
        protected override void InternalRemove(IEntity toremove)
        {
            ContainedEntity = null;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            ContainedEntity?.Delete();
        }
    }
}
