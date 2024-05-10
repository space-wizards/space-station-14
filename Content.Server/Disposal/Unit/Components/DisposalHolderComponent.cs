using Content.Server.Atmos;
using Content.Server.Disposal.Tube.Components;
using Robust.Shared.Containers;

namespace Content.Server.Disposal.Unit.Components
{
    [RegisterComponent]
    public sealed partial class DisposalHolderComponent : Component, IGasMixtureHolder
    {
        public Container Container = null!;

        /// <summary>
        ///     The total amount of time that it will take for this entity to
        ///     be pushed to the next tube
        /// </summary>
        [ViewVariables]
        public float StartingTime { get; set; }

        /// <summary>
        ///     Time left until the entity is pushed to the next tube
        /// </summary>
        [ViewVariables]
        public float TimeLeft { get; set; }

        [ViewVariables]
        public EntityUid? PreviousTube { get; set; }

        [ViewVariables]
        public Direction PreviousDirection { get; set; } = Direction.Invalid;

        [ViewVariables]
        public Direction PreviousDirectionFrom => (PreviousDirection == Direction.Invalid) ? Direction.Invalid : PreviousDirection.GetOpposite();

        [ViewVariables]
        public EntityUid? CurrentTube { get; set; }

        // CurrentDirection is not null when CurrentTube isn't null.
        [ViewVariables]
        public Direction CurrentDirection { get; set; } = Direction.Invalid;

        /// <summary>Mistake prevention</summary>
        [ViewVariables]
        public bool IsExitingDisposals { get; set; } = false;

        /// <summary>
        ///     A list of tags attached to the content, used for sorting
        /// </summary>
        [ViewVariables]
        public HashSet<string> Tags { get; set; } = new();

        [DataField("air")]
        public GasMixture Air { get; set; } = new(70);
    }
}
