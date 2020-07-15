using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalJunctionComponent : DisposalTubeComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        /// <summary>
        ///     The angles to connect to in radians.
        ///     Parsed from YAML files as degrees.
        /// </summary>
        private double[] _angles;

        public override string Name => "DisposalJunction";

        public override Direction[] ConnectableDirections()
        {
            var direction = Owner.Transform.LocalRotation;

            return _angles.Select(radian => new Angle(direction.Theta + radian).GetDir()).ToArray();
        }

        public override Direction NextDirection(DisposableComponent disposable)
        {
            var next = Owner.Transform.LocalRotation;
            var directions = ConnectableDirections().Skip(1).ToArray();

            if (Connected.TryGetValue(next.GetDir(), out var forwardTube) &&
                disposable.PreviousTube == forwardTube)
            {
                return _random.Pick(directions);
            }

            return next.GetDir();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var degrees = new List<double>();
            serializer.DataField(ref degrees, "angles", null);

            _angles = degrees.Select(MathHelper.DegreesToRadians).ToArray();
        }
    }
}
