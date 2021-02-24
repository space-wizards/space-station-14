using System;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IClimbable))]
    public class ClimbableComponent : SharedClimbableComponent
    {
        /// <summary>
        ///     The time it takes to climb onto the entity.
        /// </summary>
        [ViewVariables]
        [DataField("delay")]
        private float _climbDelay = 0.8f;

        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.EnsureComponent(out PhysicsComponent _))
            {
                Logger.Warning($"Entity {Owner.Name} at {Owner.Transform.MapPosition} didn't have a {nameof(PhysicsComponent)}");
            }
        }

        public override bool CanDragDropOn(DragDropEventArgs eventArgs)
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
        private bool CanVault(IEntity user, IEntity target, out string reason)
        {
            if (!ActionBlockerSystem.CanInteract(user))
            {
                reason = Loc.GetString("You can't do that!");
                return false;
            }

            if (!user.HasComponent<ClimbingComponent>() ||
                !user.TryGetComponent(out IBody body))
            {
                reason = Loc.GetString("You are incapable of climbing!");
                return false;
            }

            if (body.GetPartsOfType(BodyPartType.Leg).Count == 0 ||
                body.GetPartsOfType(BodyPartType.Foot).Count == 0)
            {
                reason = Loc.GetString("You are unable to climb!");
                return false;
            }

            if (!user.InRangeUnobstructed(target, Range))
            {
                reason = Loc.GetString("You can't reach there!");
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
        private bool CanVault(IEntity user, IEntity dragged, IEntity target, out string reason)
        {
            if (!ActionBlockerSystem.CanInteract(user))
            {
                reason = Loc.GetString("You can't do that!");
                return false;
            }

            if (target == null || !dragged.HasComponent<ClimbingComponent>())
            {
                reason = Loc.GetString("You can't do that!");
                return false;
            }

            bool Ignored(IEntity entity) => entity == target || entity == user || entity == dragged;

            if (!user.InRangeUnobstructed(target, Range, predicate: Ignored) ||
                !user.InRangeUnobstructed(dragged, Range, predicate: Ignored))
            {
                reason = Loc.GetString("You can't reach there!");
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public override bool DragDropOn(DragDropEventArgs eventArgs)
        {
            if (eventArgs.User == eventArgs.Dragged)
            {
                TryClimb(eventArgs.User);
            }
            else
            {
                TryMoveEntity(eventArgs.User, eventArgs.Dragged);
            }

            return true;
        }

        private async void TryMoveEntity(IEntity user, IEntity entityToMove)
        {
            var doAfterEventArgs = new DoAfterEventArgs(user, _climbDelay, default, entityToMove)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true
            };

            var result = await EntitySystem.Get<DoAfterSystem>().DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled && entityToMove.TryGetComponent(out IPhysicsComponent body) && body.PhysicsShapes.Count >= 1)
            {
                var direction = (Owner.Transform.WorldPosition - entityToMove.Transform.WorldPosition).Normalized;
                var endPoint = Owner.Transform.WorldPosition;

                var climbMode = entityToMove.GetComponent<ClimbingComponent>();
                climbMode.IsClimbing = true;

                if (MathF.Abs(direction.X) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
                {
                    endPoint = new Vector2(entityToMove.Transform.WorldPosition.X, endPoint.Y);
                }
                else if (MathF.Abs(direction.Y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
                {
                    endPoint = new Vector2(endPoint.X, entityToMove.Transform.WorldPosition.Y);
                }

                climbMode.TryMoveTo(entityToMove.Transform.WorldPosition, endPoint);
                // we may potentially need additional logic since we're forcing a player onto a climbable
                // there's also the cases where the user might collide with the person they are forcing onto the climbable that i haven't accounted for

                var othersMessage = Loc.GetString("{0:theName} forces {1:theName} onto {2:theName}!", user,
                    entityToMove, Owner);
                user.PopupMessageOtherClients(othersMessage);

                var selfMessage = Loc.GetString("You force {0:theName} onto {1:theName}!", entityToMove, Owner);
                user.PopupMessage(selfMessage);
            }
        }

        private async void TryClimb(IEntity user)
        {
            if (!user.TryGetComponent(out ClimbingComponent climbingComponent) || climbingComponent.IsClimbing)
                return;

            var doAfterEventArgs = new DoAfterEventArgs(user, _climbDelay, default, Owner)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true
            };

            var result = await EntitySystem.Get<DoAfterSystem>().DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled && user.TryGetComponent(out IPhysicsComponent body) && body.PhysicsShapes.Count >= 1)
            {
                var direction = (Owner.Transform.WorldPosition - user.Transform.WorldPosition).Normalized;
                var endPoint = Owner.Transform.WorldPosition;

                var climbMode = user.GetComponent<ClimbingComponent>();
                climbMode.IsClimbing = true;

                if (MathF.Abs(direction.X) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
                {
                    endPoint = new Vector2(user.Transform.WorldPosition.X, endPoint.Y);
                }
                else if (MathF.Abs(direction.Y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
                {
                    endPoint = new Vector2(endPoint.X, user.Transform.WorldPosition.Y);
                }

                climbMode.TryMoveTo(user.Transform.WorldPosition, endPoint);

                var othersMessage = Loc.GetString("{0:theName} jumps onto {1:theName}!", user, Owner);
                user.PopupMessageOtherClients(othersMessage);

                var selfMessage = Loc.GetString("You jump onto {0:theName}!", Owner);
                user.PopupMessage(selfMessage);
            }
        }

        /// <summary>
        ///     Allows you to vault an object with the ClimbableComponent through right click
        /// </summary>
        [Verb]
        private sealed class ClimbVerb : Verb<ClimbableComponent>
        {
            protected override void GetData(IEntity user, ClimbableComponent component, VerbData data)
            {
                if (!component.CanVault(user, component.Owner, out var _))
                {
                    data.Visibility = VerbVisibility.Invisible;
                }

                data.Text = Loc.GetString("Vault");
            }

            protected override void Activate(IEntity user, ClimbableComponent component)
            {
                component.TryClimb(user);
            }
        }
    }
}
