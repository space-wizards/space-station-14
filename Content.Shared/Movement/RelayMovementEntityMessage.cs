using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Movement
{
    [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
    public class RelayMovementEntityMessage : ComponentMessage
    {
        [PublicAPI]
        public readonly IEntity Entity;

        public RelayMovementEntityMessage(IEntity entity)
        {
            Entity = entity;
        }
    }
}
