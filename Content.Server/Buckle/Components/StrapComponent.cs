using System.Collections.Generic;
using System.Linq;
using Content.Shared.Acts;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Buckle.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrapComponent))]
    public class StrapComponent : SharedStrapComponent, IInteractHand, ISerializationHooks, IDestroyAct
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly HashSet<EntityUid> _buckledEntities = new();

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

        /// <summary>
        /// The buckled entity will be offset by this amount from the center of the strap object.
        /// If this offset it too big, it will be clamped to <see cref="MaxBuckleDistance"/>
        /// </summary>
        [DataField("buckleOffset", required: false)]
        private Vector2 _buckleOffset = Vector2.Zero;

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
        /// The distance above which a buckled entity will be automatically unbuckled.
        /// Don't change it unless you really have to
        /// </summary>
        [DataField("maxBuckleDistance", required: false)]
        public float MaxBuckleDistance = 0.1f;

        /// <summary>
        /// You can specify the offset the entity will have after unbuckling.
        /// </summary>
        [DataField("unbuckleOffset", required: false)]
        public Vector2 UnbuckleOffset = Vector2.Zero;

        /// <summary>
        /// Gets and clamps the buckle offset to MaxBuckleDistance
        /// </summary>
        public Vector2 BuckleOffset => Vector2.Clamp(
            _buckleOffset,
            Vector2.One * -MaxBuckleDistance,
            Vector2.One * MaxBuckleDistance);

        /// <summary>
        /// The entity that is currently buckled here, synced from <see cref="BuckleComponent.BuckledTo"/>
        /// </summary>
        public IReadOnlyCollection<EntityUid> BuckledEntities => _buckledEntities;

        /// <summary>
        /// The change in position to the strapped mob
        /// </summary>
        [DataField("position")]
        public StrapPosition Position { get; } = StrapPosition.None;

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

            if (!_buckledEntities.Add(buckle.Owner))
            {
                return false;
            }

            _occupiedSize += buckle.Size;

            if(_entityManager.TryGetComponent<AppearanceComponent>(buckle.Owner, out var appearanceComponent))
                appearanceComponent.SetData(StrapVisuals.RotationAngle, _rotation);

            // Update the visuals of the strap object
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<AppearanceComponent>(Owner, out var appearance))
            {
                appearance.SetData("StrapState", true);
            }

#pragma warning disable 618
            SendMessage(new StrapMessage(buckle.Owner, Owner));
#pragma warning restore 618

            return true;
        }

        /// <summary>
        ///     Removes a buckled entity.
        ///     Called from <see cref="BuckleComponent.TryUnbuckle"/>
        /// </summary>
        /// <param name="buckle">The component to remove</param>
        public void Remove(BuckleComponent buckle)
        {
            if (_buckledEntities.Remove(buckle.Owner))
            {
                if (IoCManager.Resolve<IEntityManager>().TryGetComponent<AppearanceComponent>(Owner, out var appearance))
                {
                    appearance.SetData("StrapState", false);
                }

                _occupiedSize -= buckle.Size;
#pragma warning disable 618
                SendMessage(new UnStrapMessage(buckle.Owner, Owner));
#pragma warning restore 618
            }
        }

        protected override void OnRemove()
        {
            base.OnRemove();

            RemoveAll();
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            RemoveAll();
        }

        private void RemoveAll()
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            foreach (var entity in _buckledEntities.ToArray())
            {
                if (entManager.TryGetComponent<BuckleComponent>(entity, out var buckle))
                {
                    buckle.TryUnbuckle(entity, true);
                }
            }

            _buckledEntities.Clear();
            _occupiedSize = 0;
        }

        public override ComponentState GetComponentState()
        {
            return new StrapComponentState(Position);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            if (!entManager.TryGetComponent<BuckleComponent>(eventArgs.User, out var buckle))
            {
                return false;
            }

            return buckle.ToggleBuckle(eventArgs.User, Owner);
        }

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            if (!entManager.TryGetComponent(eventArgs.Dragged, out BuckleComponent? buckleComponent)) return false;
            return buckleComponent.TryBuckle(eventArgs.User, Owner);
        }
    }
}
