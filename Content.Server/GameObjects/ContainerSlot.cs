using System;
using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects
{
    public class ContainerSlot : BaseContainer
    {
        [ViewVariables]
        public IEntity ContainedEntity
        {
            get => _containedEntity;
            private set
            {
                _containedEntity = value;
                _containedArray[0] = value;
            }
        }

        private readonly IEntity[] _containedArray = new IEntity[1];
        private IEntity _containedEntity;

        /// <inheritdoc />
        public override IReadOnlyList<IEntity> ContainedEntities
        {
            get
            {
                if (ContainedEntity == null)
                {
                    return Array.Empty<IEntity>();
                }

                return _containedArray;
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
