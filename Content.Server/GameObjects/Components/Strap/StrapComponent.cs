using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
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
    public class StrapComponent : SharedStrapComponent
    {
        private StrapPosition _position;
        private string _buckleSound;
        private string _unbuckleSound;
        private int _rotation;
        private int _size;

        /// <summary>
        /// The entity that is currently buckled here, synced from <see cref="BuckleComponent.BuckledTo"/>
        /// </summary>
        public HashSet<IEntity> BuckledEntities { get; private set; }

        /// <summary>
        /// The change in position to the strapped mob
        /// </summary>
        public override StrapPosition Position
        {
            get => _position;
            set
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
        public int OccupiedSize { get; private set; }

        public bool HasSpace(BuckleComponent buckle)
        {
            return OccupiedSize + buckle.Size <= _size;
        }

        public bool TryAddEntity(BuckleComponent buckle, bool force = false)
        {
            if (!force && !HasSpace(buckle))
            {
                return false;
            }

            BuckledEntities.Add(buckle.Owner);
            OccupiedSize += buckle.Size;

            if (buckle.Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(StrapVisuals.RotationAngle, _rotation);
            }

            return true;
        }

        public bool TryRemoveEntity(BuckleComponent buckle, bool force = false)
        {
            var removed = BuckledEntities.Remove(buckle.Owner);

            if (removed)
            {
                OccupiedSize -= buckle.Size;
            }

            return removed;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _position, "position", StrapPosition.None);
            serializer.DataField(ref _buckleSound, "buckleSound", "/Audio/effects/buckle.ogg");
            serializer.DataField(ref _unbuckleSound, "unbuckleSound", "/Audio/effects/unbuckle.ogg");
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
                    buckle.ForceUnbuckle();
                }
            }

            BuckledEntities.Clear();
            OccupiedSize = 0;
        }

        [Verb]
        private sealed class StrapVerb : Verb<StrapComponent>
        {
            protected override void GetData(IEntity user, StrapComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(component.Owner) ||
                    !user.TryGetComponent(out BuckleComponent buckle) ||
                    buckle.BuckledTo != null && buckle.BuckledTo != component.Owner)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                var userPosition = user.Transform.MapPosition;
                var strapPosition = component.Owner.Transform.MapPosition;
                var range = SharedInteractionSystem.InteractionRange / 2;
                var inRange = EntitySystem.Get<SharedInteractionSystem>()
                    .InRangeUnobstructed(userPosition, strapPosition, range,
                        predicate: entity => entity == user || entity == component.Owner);

                if (!inRange)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

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
