using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposalJunctionComponent : DisposalTubeComponent
    {
        public override string Name => "DisposalJunction";

        protected override Direction[] ConnectableDirections()
        {
            var direction = Owner.Transform.LocalRotation;
            var opposite = new Angle(direction.Theta + Math.PI);
            var side = new Angle(direction.Theta - Math.PI / 2);

            return new[] {direction.GetDir(), opposite.GetDir(), side.GetDir()};
        }

        public override Direction NextDirection(InDisposalsComponent inDisposals)
        {
            return Owner.Transform.LocalRotation.GetDir();
        }
    }
}
