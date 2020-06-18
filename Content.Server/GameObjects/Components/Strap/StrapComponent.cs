using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Strap;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
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

        /// <summary>
        /// The entity that is currently buckled here, synced from <see cref="BuckleableComponent.BuckledTo"/>
        /// </summary>
        [CanBeNull] public IEntity BuckledEntity { get; private set; }

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

        public void AddEntity(IEntity entity)
        {
            BuckledEntity = entity;
        }

        public void RemoveEntity(IEntity entity)
        {
            if (BuckledEntity == entity)
            {
                BuckledEntity = null;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _position, "position", StrapPosition.None);
            serializer.DataField(ref _buckleSound, "buckleSound", "/Audio/effects/buckle.ogg");
            serializer.DataField(ref _unbuckleSound, "unbuckleSound", "/Audio/effects/unbuckle.ogg");
        }

        public override void OnRemove()
        {
            base.OnRemove();

            if (BuckledEntity != null && BuckledEntity.TryGetComponent(out BuckleableComponent buckle))
            {
                buckle.ForceUnbuckle();
            }
        }

        [Verb]
        private sealed class StrapVerb : Verb<StrapComponent>
        {
            protected override void GetData(IEntity user, StrapComponent component, VerbData data)
            {
                var strapPosition = component.Owner.Transform.MapPosition;
                var range = SharedInteractionSystem.InteractionRange / 2;

                if (!ActionBlockerSystem.CanInteract(component.Owner) ||
                    !user.TryGetComponent(out BuckleableComponent buckle) ||
                    buckle.BuckledTo != null && buckle.BuckledTo != component.Owner ||
                    !InteractionChecks.InRangeUnobstructed(user, strapPosition, range))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = buckle.BuckledTo == null ? Loc.GetString("Buckle") : Loc.GetString("Unbuckle");
            }

            protected override void Activate(IEntity user, StrapComponent component)
            {
                if (!user.TryGetComponent(out BuckleableComponent buckle))
                {
                    return;
                }

                buckle.ToggleBuckle(user, component.Owner);
            }
        }
    }
}
