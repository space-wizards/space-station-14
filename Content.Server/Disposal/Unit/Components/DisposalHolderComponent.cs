using Content.Server.Atmos;
using Content.Server.Disposal.Tube.Components;
using Content.Shared.Body.Components;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Disposal.Unit.Components
{
    // TODO: Add gas
    [RegisterComponent]
    public sealed class DisposalHolderComponent : Component, IGasMixtureHolder
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

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
        public IDisposalTubeComponent? PreviousTube { get; set; }

        [ViewVariables]
        public Direction PreviousDirection { get; set; } = Direction.Invalid;

        [ViewVariables]
        public Direction PreviousDirectionFrom => (PreviousDirection == Direction.Invalid) ? Direction.Invalid : PreviousDirection.GetOpposite();

        [ViewVariables]
        public IDisposalTubeComponent? CurrentTube { get; set; }

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
        public GasMixture Air { get; set; } = new (70);

        protected override void Initialize()
        {
            base.Initialize();

            Container = ContainerHelpers.EnsureContainer<Container>(Owner, nameof(DisposalHolderComponent));
        }

        private bool CanInsert(EntityUid entity)
        {
            if (!Container.CanInsert(entity))
            {
                return false;
            }

            return _entMan.HasComponent<ItemComponent>(entity) ||
                   _entMan.HasComponent<BodyComponent>(entity);
        }

        public bool TryInsert(EntityUid entity)
        {
            if (!CanInsert(entity) || !Container.Insert(entity))
            {
                return false;
            }

            if (_entMan.TryGetComponent(entity, out PhysicsComponent? physics))
            {
                _entMan.System<SharedPhysicsSystem>().SetCanCollide(entity, false, body: physics);
            }

            return true;
        }
    }
}
