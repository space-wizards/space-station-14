#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldComponent : Component, IStartCollide
    {
        public override string Name => "ContainmentField";
        public ContainmentFieldConnection? Parent;

        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
        {
            if (Parent == null)
            {
                Owner.Delete();
                return;
            }

            Parent.TryRepell(Owner, otherFixture.Body.Owner);
        }
    }
}
