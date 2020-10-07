#nullable enable
using System;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldComponent : Component, ICollideBehavior
    {
        public override string Name => "ContainmentField";
        public ContainmentFieldConnection? Parent;

        public void CollideWith(IEntity collidedWith)
        {
            if (Parent == null)
            {
                Owner.Delete();
                return;
            }

            Parent.TryRepell(Owner, collidedWith);
        }
    }
}
