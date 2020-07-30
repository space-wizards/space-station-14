using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Disposal
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
            var previousTube = holder.PreviousTube;
            var forward = directions[0];
            if (previousTube == null || !Connected.ContainsValue(previousTube))
            {
                return forward;
            }

            var forwardTube = Connected.GetValueOrDefault(forward);
            var backward = directions[1];
            return previousTube == forwardTube ? backward : forward;
        }
    }
}
