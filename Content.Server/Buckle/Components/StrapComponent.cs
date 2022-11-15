using System.Linq;
using Content.Server.Buckle.Systems;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.DragDrop;
using Robust.Shared.Audio;

namespace Content.Server.Buckle.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrapComponent))]
    public sealed class StrapComponent : SharedStrapComponent
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        /// <summary>
        /// The angle in degrees to rotate the player by when they get strapped
        /// </summary>
        [ViewVariables] [DataField("rotation")]
        private int _rotation;

        /// <summary>
        /// The size of the strap which is compared against when buckling entities
        /// </summary>
        [ViewVariables] [DataField("size")] private int _size = 100;
        private int _occupiedSize;

        private bool _enabled = true;

        /// <summary>
        /// If disabled, nothing can be buckled on this object, and it will unbuckle anything that's already buckled
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (_enabled == value) return;
                RemoveAll();
            }
        }

        /// <summary>
        /// You can specify the offset the entity will have after unbuckling.
        /// </summary>
        [DataField("unbuckleOffset", required: false)]
        public Vector2 UnbuckleOffset = Vector2.Zero;
        /// <summary>
        /// The sound to be played when a mob is buckled
        /// </summary>
        [ViewVariables]
        [DataField("buckleSound")]
        public SoundSpecifier BuckleSound { get; } = new SoundPathSpecifier("/Audio/Effects/buckle.ogg");

        /// <summary>
        /// The sound to be played when a mob is unbuckled
        /// </summary>
        [ViewVariables]
        [DataField("unbuckleSound")]
        public SoundSpecifier UnbuckleSound { get; } = new SoundPathSpecifier("/Audio/Effects/unbuckle.ogg");

        /// <summary>
        /// ID of the alert to show when buckled
        /// </summary>
        [ViewVariables]
        [DataField("buckledAlertType")]
        public AlertType BuckledAlertType { get; } = AlertType.Buckled;

        /// <summary>
        /// The sum of the sizes of all the buckled entities in this strap
        /// </summary>
        [ViewVariables]
        public int OccupiedSize => _occupiedSize;

        /// <summary>
        ///     Checks if this strap has enough space for a new occupant.
        /// </summary>
        /// <param name="buckle">The new occupant</param>
        /// <returns>true if there is enough space, false otherwise</returns>
        public bool HasSpace(BuckleComponent buckle)
        {
            return OccupiedSize + buckle.Size <= _size;
        }

        /// <summary>
        ///     DO NOT CALL THIS DIRECTLY.
        ///     Adds a buckled entity. Called from <see cref="BuckleComponent.TryBuckle"/>
        /// </summary>
        /// <param name="buckle">The component to add</param>
        /// <param name="force">
        ///     Whether or not to check if the strap has enough space
        /// </param>
        /// <returns>True if added, false otherwise</returns>
        public bool TryAdd(BuckleComponent buckle, bool force = false)
        {
            if (!Enabled) return false;

            if (!force && !HasSpace(buckle))
            {
                return false;
            }

            if (!BuckledEntities.Add(buckle.Owner))
            {
                return false;
            }

            _occupiedSize += buckle.Size;

            if(_entityManager.TryGetComponent<AppearanceComponent>(buckle.Owner, out var appearanceComponent))
                appearanceComponent.SetData(StrapVisuals.RotationAngle, _rotation);

            // Update the visuals of the strap object
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<AppearanceComponent>(Owner, out var appearance))
            {
                appearance.SetData(StrapVisuals.State, true);
            }

            Dirty();
            return true;
        }

        /// <summary>
        ///     Removes a buckled entity.
        ///     Called from <see cref="BuckleComponent.TryUnbuckle"/>
        /// </summary>
        /// <param name="buckle">The component to remove</param>
        public void Remove(BuckleComponent buckle)
        {
            if (BuckledEntities.Remove(buckle.Owner))
            {
                if (IoCManager.Resolve<IEntityManager>().TryGetComponent<AppearanceComponent>(Owner, out var appearance))
                {
                    appearance.SetData(StrapVisuals.State, false);
                }

                _occupiedSize -= buckle.Size;
                Dirty();
            }
        }

        protected override void OnRemove()
        {
            base.OnRemove();

            RemoveAll();
        }

        public void RemoveAll()
        {
            var buckleSystem = IoCManager.Resolve<IEntityManager>().System<BuckleSystem>();

            foreach (var entity in BuckledEntities.ToArray())
            {
                buckleSystem.TryUnbuckle(entity, entity, true);
            }

            BuckledEntities.Clear();
            _occupiedSize = 0;
            Dirty();
        }

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            var buckleSystem = IoCManager.Resolve<IEntityManager>().System<BuckleSystem>();
            return buckleSystem.TryBuckle(eventArgs.Dragged, eventArgs.User, Owner);
        }
    }
}
