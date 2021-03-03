#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldComponent : Component, ICollideBehavior
    {
        public override string Name => "ContainmentField";
        public ContainmentFieldConnection? Parent;

        public void CollideWith(IPhysBody ourBody, IPhysBody otherBody, in Manifold manifold)
        {
            if (Parent == null)
            {
                Owner.Delete();
                return;
            }

            Parent.TryRepell(Owner, otherBody.Entity);
        }
    }
}
