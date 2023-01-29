using System.Linq;
using Content.Server.Disposal.Unit.Components;
using Robust.Shared.Random;

namespace Content.Server.Disposal.Tube.Components
{
    [Virtual]
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    [ComponentReference(typeof(DisposalTubeComponent))]
    public class DisposalJunctionComponent : DisposalTubeComponent
    {
        public override string ContainerId => "DisposalJunction";

        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        /// <summary>
        ///     The angles to connect to.
        /// </summary>
        [DataField("degrees")]
        private List<Angle> _degrees = new();

        protected override Direction[] ConnectableDirections()
        {
            var direction = _entMan.GetComponent<TransformComponent>(Owner).LocalRotation;

            return _degrees.Select(degree => new Angle(degree.Theta + direction.Theta).GetDir()).ToArray();
        }

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            var next = _entMan.GetComponent<TransformComponent>(Owner).LocalRotation.GetDir();
            var directions = ConnectableDirections().Skip(1).ToArray();

            if (holder.PreviousDirectionFrom == Direction.Invalid ||
                holder.PreviousDirectionFrom == next)
            {
                return _random.Pick(directions);
            }

            return next;
        }
    }
}
