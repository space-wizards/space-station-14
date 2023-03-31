using Content.Server.Disposal.Unit.Components;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    [ComponentReference(typeof(DisposalTubeComponent))]
    public sealed class DisposalBendComponent : DisposalTubeComponent
    {
        public override string ContainerId => "DisposalBend";

        [DataField("sideDegrees")]
        private int _sideDegrees = -90;

        protected override Direction[] ConnectableDirections()
        {
            var direction = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).LocalRotation;
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
