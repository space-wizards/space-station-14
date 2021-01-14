#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Buckle;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Strap;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Strap
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrapComponent))]
    public class StrapComponent : SharedStrapComponent, IInteractHand
    {
        [ComponentDependency] public readonly SpriteComponent? SpriteComponent = null;

        private HashSet<IEntity> _buckledEntities = null!;
        private StrapPosition _position;
        private string _buckleSound = null!;
        private string _unbuckleSound = null!;
        private AlertType _buckledAlertType;

        /// <summary>
        /// The angle in degrees to rotate the player by when they get strapped
        /// </summary>
        [ViewVariables]
        private int _rotation;

        /// <summary>
        /// The size of the strap which is compared against when buckling entities
        /// </summary>
        [ViewVariables]
        private int _size;
        private int _occupiedSize;

        /// <summary>
        /// The entity that is currently buckled here, synced from <see cref="BuckleComponent.BuckledTo"/>
        /// </summary>
        public IReadOnlyCollection<IEntity> BuckledEntities => _buckledEntities;

        /// <summary>
        /// The change in position to the strapped mob
        /// </summary>
        public StrapPosition Position => _position;

        /// <summary>
        /// The sound to be played when a mob is buckled
        /// </summary>
        [ViewVariables]
        public string BuckleSound => _buckleSound;

        /// <summary>
        /// The sound to be played when a mob is unbuckled
        /// </summary>
        [ViewVariables]
        public string UnbuckleSound => _unbuckleSound;

        /// <summary>
        /// ID of the alert to show when buckled
        /// </summary>
        [ViewVariables]
        public AlertType BuckledAlertType => _buckledAlertType;

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

            buckle.AppearanceComponent?.SetData(StrapVisuals.RotationAngle, _rotation);

            SendMessage(new StrapMessage(buckle.Owner, Owner));

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
                SendMessage(new UnStrapMessage(buckle.Owner, Owner));
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _position, "position", StrapPosition.None);
            serializer.DataField(ref _buckleSound, "buckleSound", "/Audio/Effects/buckle.ogg");
            serializer.DataField(ref _unbuckleSound, "unbuckleSound", "/Audio/Effects/unbuckle.ogg");
            serializer.DataField(ref _buckledAlertType, "buckledAlertType", AlertType.Buckled);
            serializer.DataField(ref _rotation, "rotation", 0);

            var defaultSize = 100;

            serializer.DataField(ref _size, "size", defaultSize);
            _buckledEntities = new HashSet<IEntity>(_size / defaultSize);

            _occupiedSize = 0;
        }

        public override void OnRemove()
        {
            base.OnRemove();

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

        public override ComponentState GetComponentState()
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

        [Verb]
        private sealed class StrapVerb : Verb<StrapComponent>
        {
            protected override void GetData(IEntity user, StrapComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(component.Owner) ||
                    !user.TryGetComponent<BuckleComponent>(out var buckle) ||
                    buckle.BuckledTo != null && buckle.BuckledTo != component ||
                    user == component.Owner)
                {
                    return;
                }

                var parent = component.Owner.Transform.Parent;
                while (parent != null)
                {
                    if (parent == user.Transform)
                    {
                        return;
                    }

                    parent = parent.Parent;
                }

                if (!user.InRangeUnobstructed(component, buckle.Range))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString(buckle.BuckledTo == null ? "Buckle" : "Unbuckle");
            }

            protected override void Activate(IEntity user, StrapComponent component)
            {
                if (!user.TryGetComponent<BuckleComponent>(out var buckle))
                {
                    return;
                }

                buckle.ToggleBuckle(user, component.Owner);
            }
        }

        public override bool DragDropOn(DragDropEventArgs eventArgs)
        {
            if (!eventArgs.Dragged.TryGetComponent(out BuckleComponent? buckleComponent)) return false;
            return buckleComponent.TryBuckle(eventArgs.User, Owner);
        }
    }
}
