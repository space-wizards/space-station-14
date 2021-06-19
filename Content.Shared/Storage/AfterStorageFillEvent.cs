using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Storage
{
    [PublicAPI]
    public sealed class AfterStorageFillEvent : EntityEventArgs
    {
        public IEntity Entity;
        public IReadOnlyList<IEntity>? ContainedEntities { get; }

        public AfterStorageFillEvent(IEntity entity, IReadOnlyList<IEntity>? containedEntities)
        {
            Entity = entity;
            ContainedEntities = containedEntities;
        }
    }
}
