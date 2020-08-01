using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalBendComponent : DisposalTubeComponent
    {
        private int _sideDegrees;

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
            var previousTube = holder.PreviousTube;

            if (previousTube == null)
            {
                return directions[0];
            }

            var previousDirection = DirectionTo(previousTube);
            return previousDirection == directions[0] ? directions[1] : directions[0];
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _sideDegrees, "sideDegrees", -90);
        }
    }
}
