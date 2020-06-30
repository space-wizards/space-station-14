using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposalJunctionComponent : DisposalTubeComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

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
            var next = Owner.Transform.LocalRotation;
            if (Connected.TryGetValue(next.GetDir(), out var forwardTube) &&
                inDisposals.PreviousTube == forwardTube)
            {
                next = _random.Prob(0.5f)
                    ? new Angle(next.Theta + Math.PI)
                    : new Angle(next.Theta - Math.PI / 2);
            }

            return next.GetDir();
        }
    }
}
