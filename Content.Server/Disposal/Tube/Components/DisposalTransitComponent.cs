using System;
using Content.Server.Disposal.Unit.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Disposal.Tube.Components
{
    // TODO: Different types of tubes eject in random direction with no exit point
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalTransitComponent : DisposalTubeComponent
    {
        public override string Name => "DisposalTransit";

        protected override Direction[] ConnectableDirections()
        {
            var rotation = Owner.Transform.LocalRotation;
            var opposite = new Angle(rotation.Theta + Math.PI);

            return new[] {rotation.GetDir(), opposite.GetDir()};
        }

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            var directions = ConnectableDirections();
            var previousDF = holder.PreviousDirectionFrom;
            var forward = directions[0];

            if (previousDF == Direction.Invalid)
            {
                return forward;
            }

            var backward = directions[1];
            return previousDF == forward ? backward : forward;
        }
    }
}
