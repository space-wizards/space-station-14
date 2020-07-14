using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Strap;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Strap
{
    [RegisterComponent]
    public class StrapComponent : SharedStrapComponent, IInteractHand
    {
        private StrapPosition _position;
        private string _buckleSound;
        private string _unbuckleSound;
        private string _buckledIcon;
        private int _rotation;
        private int _size;

        /// <summary>
        /// The entity that is currently buckled here, synced from <see cref="BuckleComponent.BuckledTo"/>
        /// </summary>
        private HashSet<IEntity> BuckledEntities { get; set; }

        /// <summary>
        /// The change in position to the strapped mob
        /// </summary>
        public override StrapPosition Position
        {
            get => _position;
            protected set
            {
                _position = value;
                Dirty();
            }
        }

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
        /// The icon to be displayed as a status when buckled
        /// </summary>
        [ViewVariables]
        public string BuckledIcon => _buckledIcon;

        /// <summary>
        /// The angle in degrees to rotate the player by when they get strapped
        /// </summary>
        [ViewVariables]
        public int Rotation => _rotation;

        /// <summary>
        /// The size of the strap which is compared against when buckling entities
        /// </summary>
        [ViewVariables]
        public int Size => _size;

        /// <summary>
        /// The sum of the sizes of all the buckled entities in this strap
        /// </summary>
        [ViewVariables]
        private int OccupiedSize { get; set; }

        public bool HasSpace(BuckleComponent buckle)
        {
            return OccupiedSize + buckle.Size <= _size;
        }

        /// <summary>
        /// Adds a buckled entity. Called from <see cref="BuckleComponent.TryBuckle"/>
        /// </summary>
        /// <param name="buckle">The component to add</param>
        /// <param name="force">Whether or not to check if the strap has enough space</param>
        /// <returns>True if added, false otherwise</returns>
        public bool TryAdd(BuckleComponent buckle, bool force = false)
        {
            if (!force && !HasSpace(buckle))
            {
                return false;
            }

            if (!BuckledEntities.Add(buckle.Owner))
            {
                return false;
            }

            OccupiedSize += buckle.Size;

            if (buckle.Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(StrapVisuals.RotationAngle, _rotation);
            }

            return true;
        }

        /// <summary>
        /// Removes a buckled entity. Called from <see cref="BuckleComponent.TryUnbuckle"/>
        /// </summary>
        /// <param name="buckle">The component to remove</param>
        public void Remove(BuckleComponent buckle)
        {
            if (BuckledEntities.Remove(buckle.Owner))
            {
                OccupiedSize -= buckle.Size;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _position, "position", StrapPosition.None);
            serializer.DataField(ref _buckleSound, "buckleSound", "/Audio/Effects/buckle.ogg");
            serializer.DataField(ref _unbuckleSound, "unbuckleSound", "/Audio/Effects/unbuckle.ogg");
            serializer.DataField(ref _buckledIcon, "buckledIcon", "/Textures/Interface/StatusEffects/Buckle/buckled.png");
            serializer.DataField(ref _rotation, "rotation", 0);

            var defaultSize = 100;

            serializer.DataField(ref _size, "size", defaultSize);
            BuckledEntities = new HashSet<IEntity>(_size / defaultSize);

            OccupiedSize = 0;
        }

        public override void OnRemove()
        {
            base.OnRemove();

            foreach (var entity in BuckledEntities)
            {
                if (entity.TryGetComponent(out BuckleComponent buckle))
                {
                    buckle.TryUnbuckle(entity, true);
                }
            }

            BuckledEntities.Clear();
            OccupiedSize = 0;
        }

        public override ComponentState GetComponentState()
        {
            return new StrapComponentState(Position);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out BuckleComponent buckle))
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
                    !user.TryGetComponent(out BuckleComponent buckle) ||
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

                var userPosition = user.Transform.MapPosition;
                var strapPosition = component.Owner.Transform.MapPosition;
                var range = SharedInteractionSystem.InteractionRange / 2;
                var inRange = EntitySystem.Get<SharedInteractionSystem>()
                    .InRangeUnobstructed(userPosition, strapPosition, range,
                        predicate: entity => entity == user || entity == component.Owner);

                if (!inRange)
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = buckle.BuckledTo == null ? Loc.GetString("Buckle") : Loc.GetString("Unbuckle");
            }

            protected override void Activate(IEntity user, StrapComponent component)
            {
                if (!user.TryGetComponent(out BuckleComponent buckle))
                {
                    return;
                }

                buckle.ToggleBuckle(user, component.Owner);
            }
        }
    }
}
