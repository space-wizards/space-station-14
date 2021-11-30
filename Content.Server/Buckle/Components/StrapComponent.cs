using System.Collections.Generic;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Acts;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Buckle.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrapComponent))]
    public class StrapComponent : SharedStrapComponent, IInteractHand, ISerializationHooks, IDestroyAct
    {
        [ComponentDependency] public readonly SpriteComponent? SpriteComponent = null;

        private readonly HashSet<IEntity> _buckledEntities = new();

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
        /// The entity that is currently buckled here, synced from <see cref="BuckleComponent.BuckledTo"/>
        /// </summary>
        public IReadOnlyCollection<IEntity> BuckledEntities => _buckledEntities;

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
            if (!force && !HasSpace(buckle))
            {
                return false;
            }

            if (!_buckledEntities.Add(buckle.Owner))
            {
                return false;
            }

            _occupiedSize += buckle.Size;

            buckle.Appearance?.SetData(StrapVisuals.RotationAngle, _rotation);

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
            foreach (var entity in _buckledEntities.ToArray())
            {
                if (entity.TryGetComponent<BuckleComponent>(out var buckle))
                {
                    buckle.TryUnbuckle(entity, true);
                }
            }

            _buckledEntities.Clear();
            _occupiedSize = 0;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new StrapComponentState(Position);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent<BuckleComponent>(out var buckle))
            {
                return false;
            }

            return buckle.ToggleBuckle(eventArgs.User, Owner);
        }

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            if (!eventArgs.Dragged.TryGetComponent(out BuckleComponent? buckleComponent)) return false;
            return buckleComponent.TryBuckle(eventArgs.User, Owner);
        }
    }
}
