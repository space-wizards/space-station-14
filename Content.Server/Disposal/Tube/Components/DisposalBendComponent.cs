using Content.Server.Disposal.Unit.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalBendComponent : DisposalTubeComponent
    {
        [DataField("sideDegrees")]
        private int _sideDegrees = -90;

        public override string Name => "DisposalBend";

        protected override Direction[] ConnectableDirections()
        {
            var direction = Owner.Transform.LocalRotation;
            var side = new Angle(MathHelper.DegreesToRadians(direction.Degrees + _sideDegrees));

            return new[] {direction.GetDir(), side.GetDir()};
        }

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            var directions = ConnectableDirections();
            var previousDF = holder.PreviousDirectionFrom;

            if (previousDF == Direction.Invalid)
            {
                return directions[0];
            }

            return previousDF == directions[0] ? directions[1] : directions[0];
        }
    }
}
