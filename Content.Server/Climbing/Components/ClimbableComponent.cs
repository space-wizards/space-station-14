using System;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Climbing;
using Content.Shared.DragDrop;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Climbing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IClimbable))]
    public class ClimbableComponent : SharedClimbableComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        /// <summary>
        ///     The time it takes to climb onto the entity.
        /// </summary>
        [ViewVariables]
        [DataField("delay")]
        private float _climbDelay = 0.8f;

        protected override void Initialize()
        {
            base.Initialize();

            if (!Owner.EnsureComponent(out PhysicsComponent _))
            {
                Logger.Warning($"Entity {_entities.GetComponent<MetaDataComponent>(Owner).EntityName} at {_entities.GetComponent<TransformComponent>(Owner).MapPosition} didn't have a {nameof(PhysicsComponent)}");
            }
        }

        public override bool CanDragDropOn(DragDropEvent eventArgs)
        {
            if (!base.CanDragDropOn(eventArgs))
                return false;

            string reason;
            bool canVault;

            if (eventArgs.User == eventArgs.Dragged)
                canVault = CanVault(eventArgs.User, eventArgs.Target, out reason);
            else
                canVault = CanVault(eventArgs.User, eventArgs.Dragged, eventArgs.Target, out reason);

            if (!canVault)
                eventArgs.User.PopupMessage(reason);

            return canVault;
        }

        /// <summary>
        /// Checks if the user can vault the target
        /// </summary>
        /// <param name="user">The entity that wants to vault</param>
        /// <param name="target">The object that is being vaulted</param>
        /// <param name="reason">The reason why it cant be dropped</param>
        /// <returns></returns>
        private bool CanVault(EntityUid user, EntityUid target, out string reason)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
            {
                reason = Loc.GetString("comp-climbable-cant-interact");
                return false;
            }

            if (!_entities.HasComponent<ClimbingComponent>(user) ||
                !_entities.TryGetComponent(user, out SharedBodyComponent? body))
            {
                reason = Loc.GetString("comp-climbable-cant-climb");
                return false;
            }

            if (!body.HasPartOfType(BodyPartType.Leg) ||
                !body.HasPartOfType(BodyPartType.Foot))
            {
                reason = Loc.GetString("comp-climbable-cant-climb");
                return false;
            }

            if (!user.InRangeUnobstructed(target, Range))
            {
                reason = Loc.GetString("comp-climbable-cant-reach");
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// Checks if the user can vault the dragged entity onto the the target
        /// </summary>
        /// <param name="user">The user that wants to vault the entity</param>
        /// <param name="dragged">The entity that is being vaulted</param>
        /// <param name="target">The object that is being vaulted onto</param>
        /// <param name="reason">The reason why it cant be dropped</param>
        /// <returns></returns>
        private bool CanVault(EntityUid user, EntityUid dragged, EntityUid target, out string reason)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
            {
                reason = Loc.GetString("comp-climbable-cant-interact");
                return false;
            }

            if (!_entities.HasComponent<ClimbingComponent>(dragged))
            {
                reason = Loc.GetString("comp-climbable-cant-climb");
                return false;
            }

            bool Ignored(EntityUid entity) => entity == target || entity == user || entity == dragged;

            if (!user.InRangeUnobstructed(target, Range, predicate: Ignored) ||
                !user.InRangeUnobstructed(dragged, Range, predicate: Ignored))
            {
                reason = Loc.GetString("comp-climbable-cant-reach");
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            if (eventArgs.User == eventArgs.Dragged)
            {
                TryClimb(eventArgs.User, eventArgs.Target);
            }
            else
            {
                TryMoveEntity(eventArgs.User, eventArgs.Dragged, eventArgs.Target);
            }

            return true;
        }

        private async void TryMoveEntity(EntityUid user, EntityUid entityToMove, EntityUid climbable)
        {
            var doAfterEventArgs = new DoAfterEventArgs(user, _climbDelay, default, entityToMove)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true
            };

            var result = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled && _entities.TryGetComponent(entityToMove, out FixturesComponent? body) && body.FixtureCount >= 1)
            {
                var entityPos = _entities.GetComponent<TransformComponent>(entityToMove).WorldPosition;

                var direction = (_entities.GetComponent<TransformComponent>(Owner).WorldPosition - entityPos).Normalized;
                var endPoint = _entities.GetComponent<TransformComponent>(Owner).WorldPosition;

                var climbMode = _entities.GetComponent<ClimbingComponent>(entityToMove);
                climbMode.IsClimbing = true;

                if (MathF.Abs(direction.X) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
                {
                    endPoint = new Vector2(entityPos.X, endPoint.Y);
                }
                else if (MathF.Abs(direction.Y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
                {
                    endPoint = new Vector2(endPoint.X, entityPos.Y);
                }

                climbMode.TryMoveTo(entityPos, endPoint);
                // we may potentially need additional logic since we're forcing a player onto a climbable
                // there's also the cases where the user might collide with the person they are forcing onto the climbable that i haven't accounted for

                _entities.EventBus.RaiseLocalEvent(entityToMove, new StartClimbEvent(climbable), false);
                _entities.EventBus.RaiseLocalEvent(climbable, new ClimbedOnEvent(entityToMove), false);

                var othersMessage = Loc.GetString("comp-climbable-user-climbs-force-other",
                    ("user", user), ("moved-user", entityToMove), ("climbable", Owner));
                user.PopupMessageOtherClients(othersMessage);

                var selfMessage = Loc.GetString("comp-climbable-user-climbs-force", ("moved-user", entityToMove), ("climbable", Owner));
                user.PopupMessage(selfMessage);
            }
        }

        public async void TryClimb(EntityUid user, EntityUid climbable)
        {
            if (!_entities.TryGetComponent(user, out ClimbingComponent? climbingComponent) || climbingComponent.IsClimbing)
                return;

            var doAfterEventArgs = new DoAfterEventArgs(user, _climbDelay, default, Owner)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true
            };

            var result = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled && _entities.TryGetComponent(user, out FixturesComponent? fixtureComp) && fixtureComp.Fixtures.Count >= 1)
            {
                // TODO: Remove the copy-paste code
                var userPos = _entities.GetComponent<TransformComponent>(user).WorldPosition;

                var direction = (_entities.GetComponent<TransformComponent>(Owner).WorldPosition - userPos).Normalized;
                var endPoint = _entities.GetComponent<TransformComponent>(Owner).WorldPosition;

                _entities.EventBus.RaiseLocalEvent(user, new StartClimbEvent(climbable), false);
                _entities.EventBus.RaiseLocalEvent(climbable, new ClimbedOnEvent(user), false);

                var climbMode = _entities.GetComponent<ClimbingComponent>(user);
                climbMode.IsClimbing = true;

                if (MathF.Abs(direction.X) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
                {
                    endPoint = new Vector2(_entities.GetComponent<TransformComponent>(user).WorldPosition.X, endPoint.Y);
                }
                else if (MathF.Abs(direction.Y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
                {
                    endPoint = new Vector2(endPoint.X, _entities.GetComponent<TransformComponent>(user).WorldPosition.Y);
                }

                climbMode.TryMoveTo(userPos, endPoint);

                var othersMessage = Loc.GetString("comp-climbable-user-climbs-other", ("user", user), ("climbable", Owner));
                user.PopupMessageOtherClients(othersMessage);

                var selfMessage = Loc.GetString("comp-climbable-user-climbs", ("climbable", Owner));
                user.PopupMessage(selfMessage);
            }
        }
    }
}

/// <summary>
///     Raised on an entity when it is climbed on.
/// </summary>
public class ClimbedOnEvent : EntityEventArgs
{
    public EntityUid Climber;

    public ClimbedOnEvent(EntityUid climber)
    {
        Climber = climber;
    }
}

/// <summary>
///     Raised on an entity when it successfully climbs on something.
/// </summary>
public class StartClimbEvent : EntityEventArgs
{
    public EntityUid Climbable;

    public StartClimbEvent(EntityUid climbable)
    {
        Climbable = climbable;
    }
}
