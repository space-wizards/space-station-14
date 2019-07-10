using System;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using System.Collections.Generic;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects
{
    public class ContainerSlot : BaseContainer
    {
        [ViewVariables]
        public IEntity ContainedEntity { get; private set; } = null;

        /// <inheritdoc />
        public override IReadOnlyCollection<IEntity> ContainedEntities
        {
            get
            {
                if (ContainedEntity == null)
                {
                    return Array.Empty<IEntity>();
                }

                return new List<IEntity> {ContainedEntity}.AsReadOnly();
            }
        }

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
            base.InternalInsert(toinsert);
        }

        /// <inheritdoc />
        protected override void InternalRemove(IEntity toremove)
        {
            ContainedEntity = null;
            base.InternalRemove(toremove);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            ContainedEntity?.Delete();
        }
    }
}
