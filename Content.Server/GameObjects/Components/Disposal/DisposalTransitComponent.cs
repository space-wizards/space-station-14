using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Disposal
{
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

        public override IDisposalTubeComponent NextTube(InDisposalsComponent inDisposals)
        {
            var directions = ConnectableDirections();
            var previous = inDisposals.PreviousTube;
            var forward = Connected.GetValueOrDefault(directions[0]);
            if (previous == null || !Connected.ContainsValue(previous))
            {
                return forward;
            }

            var backward = Connected.GetValueOrDefault(directions[1]);
            return previous == forward ? backward : forward;
        }
    }
}
