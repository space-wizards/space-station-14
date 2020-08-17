
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Server.Interfaces.Player;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Movement;
using Content.Server.Health.BodySystem;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects.Components.Damage;
using System;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class ClimbableComponent : SharedClimbableComponent, IDragDropOn
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
#pragma warning restore 649

        /// <summary>
        ///     The range from which this entity can be climbed.
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

                var damageable = eventArgs.Dropped.GetComponent<IDamageableComponent>();

                if (damageable.CurrentDamageState is NormalState) // todo: this should also work on people who are unconscious/restrained even if their damage state is NormalState
                {
                    _notifyManager.PopupMessage(eventArgs.Dropped, eventArgs.User, Loc.GetString("You struggle to move {0:them}, but they resist!", eventArgs.Dropped));
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.Dropped, Loc.GetString("You resist {0:them}'s attempts to move you!", eventArgs.User));

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
                var direction = (Owner.Transform.WorldPosition - user.Transform.WorldPosition).Normalized;
                var endPoint = Owner.Transform.WorldPosition;

                var climbMode = user.GetComponent<ClimbModeComponent>();
                climbMode.SetClimbing(true);

                if (MathF.Abs(direction.X) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
                {
                    endPoint = new Robust.Shared.Maths.Vector2(user.Transform.WorldPosition.X, endPoint.Y);
                }
                else if (MathF.Abs(direction.Y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
                {
                    endPoint = new Robust.Shared.Maths.Vector2(endPoint.X, user.Transform.WorldPosition.Y);
                }

                climbMode.TryMoveTo(user.Transform.WorldPosition, endPoint);

                PopupMessageOtherClientsInRange(user, Loc.GetString("{0:them} jumps onto the {1}!", user, Owner), 15);
                _notifyManager.PopupMessage(user, user, Loc.GetString("You jump onto {0:theName}!", Owner));

                return true;
            }

            return false;
        }

        private void PopupMessageOtherClientsInRange(IEntity source, string message, int maxReceiveDistance)
        {
            var viewers = _playerManager.GetPlayersInRange(source.Transform.GridPosition, maxReceiveDistance);

            foreach (var viewer in viewers)
            {
                var viewerEntity = viewer.AttachedEntity;

                if (viewerEntity == null || source == viewerEntity)
                {
                    continue;
                }

                source.PopupMessage(viewer.AttachedEntity, message);
            }
        }
    }
}
