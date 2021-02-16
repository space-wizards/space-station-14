#nullable enable
using Robust.Shared.GameObjects;

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
