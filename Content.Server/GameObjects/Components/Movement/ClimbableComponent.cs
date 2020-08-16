using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Content.Shared.GameObjects.Components.Movement;
using Content.Server.Health.BodySystem;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class ClimbableComponent : SharedClimbableComponent, IDragDropOn
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
#pragma warning restore 649

        private const float CLIMB_DURATION = 0.3f;

        /// <summary>
        ///     The range from which this entity can be climbed.>.
        /// </summary>
        [ViewVariables]
        private float _range;

        private ICollidableComponent _collidableComponent;

        public override void Initialize()
        {
            base.Initialize();

            _collidableComponent = Owner.GetComponent<ICollidableComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "range", SharedInteractionSystem.InteractionRange / 1.4f);
        }

        bool IDragDropOn.CanDragDropOn(DragDropEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't do that!"));

                return false;
            }

            if (!eventArgs.User.HasComponent<HandsComponent>())
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You don't have hands!"));

                return false;
            }

            if (eventArgs.User == eventArgs.Dropped) // user is dragging themselves onto a climbable
            {
                if (eventArgs.Target == null) 
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't climb that!"));

                    return false;
                }

                if (!eventArgs.User.HasComponent<ClimbModeComponent>())
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You are incapable of climbing!"));

                    return false;
                }

                var species = eventArgs.User.GetComponent<SpeciesComponent>();
                
                if (!(species.CurrentDamageState is NormalState)) // players who aren't healthy and upright can't jump on tables.
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You are unable to climb!"));

                    return false;
                }

                var bodyManager = eventArgs.User.GetComponent<BodyManagerComponent>();

                if (bodyManager.GetBodyPartsOfType(Shared.Health.BodySystem.BodyPartType.Leg).Count == 0 ||
                    bodyManager.GetBodyPartsOfType(Shared.Health.BodySystem.BodyPartType.Foot).Count == 0) 
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You are unable to climb!"));

                    return false;
                }

                var userPosition = eventArgs.User.Transform.MapPosition;
                var climbablePosition = eventArgs.Target.Transform.MapPosition;
                var interaction = EntitySystem.Get<SharedInteractionSystem>();
                bool Ignored(IEntity entity) => (entity == eventArgs.Target || entity == eventArgs.User);

                if (!interaction.InRangeUnobstructed(userPosition, climbablePosition, _range, predicate: Ignored))
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't reach there!"));

                    return false;
                }
            }
            else // user is dragging some other entity onto a climbable
            {
                if (eventArgs.Target == null || !eventArgs.Dropped.HasComponent<ClimbModeComponent>()) 
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't do that!"));
                    
                    return false;
                }

                var species = eventArgs.Dropped.GetComponent<SpeciesComponent>();

                if (species.CurrentDamageState is NormalState) // todo: this should also work on people who are unconscious/restrained even if their damage state is NormalState
                {
                    _notifyManager.PopupMessage(eventArgs.Dropped, eventArgs.User, Loc.GetString("You struggle to move {0:them}, but they resist!", eventArgs.Dropped));
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.Dropped, Loc.GetString("You resist {0:them}'s attempts to move you!", eventArgs.User));

                    return false;
                }

                species = eventArgs.User.GetComponent<SpeciesComponent>();

                if (!(species.CurrentDamageState is NormalState)) // players who aren't healthy and upright can't put other people on tables
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You are too weak to move {0:them}!", eventArgs.Dropped));

                    return false;
                }

                var userPosition = eventArgs.User.Transform.MapPosition;
                var otherUserPosition = eventArgs.Dropped.Transform.MapPosition;
                var climbablePosition = eventArgs.Target.Transform.MapPosition;
                var interaction = EntitySystem.Get<SharedInteractionSystem>();
                bool Ignored(IEntity entity) => (entity == eventArgs.Target || entity == eventArgs.User || entity == eventArgs.Dropped);

                if (!interaction.InRangeUnobstructed(userPosition, climbablePosition, _range, predicate: Ignored) ||
                    !interaction.InRangeUnobstructed(userPosition, otherUserPosition, _range, predicate: Ignored))
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't reach there!"));

                    return false;
                }
            }

            return true;
        }

        bool IDragDropOn.DragDropOn(DragDropEventArgs eventArgs)
        {
            return TryClimb(eventArgs.Dropped);
        }

        bool TryClimb(IEntity user)
        {
            if (user.TryGetComponent(out ICollidableComponent body) && body.PhysicsShapes.Count >= 1)
            {
                body.PhysicsShapes[0].CollisionMask &= ~((int) CollisionGroup.VaultImpassable);
                user.Transform.WorldPosition = Owner.Transform.WorldPosition; // todo: can this get lerped somehow?

                var climbMode = user.GetComponent<ClimbModeComponent>();
                climbMode.SetClimbing(true);

                return true;
            }

            return false;
        }
    }
}
