using System;
using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IClimbable))]
    public class ClimbableComponent : SharedClimbableComponent, IDragDropOn
    {
        /// <summary>
        ///     The range from which this entity can be climbed.
        /// </summary>
        [ViewVariables]
        private float _range;

        /// <summary>
        ///     The time it takes to climb onto the entity.
        /// </summary>
        [ViewVariables]
        private float _climbDelay;

        private DoAfterSystem _doAfterSystem;

        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.EnsureComponent(out CollidableComponent _))
            {
                Logger.Warning($"Entity {Owner.Name} at {Owner.Transform.MapPosition} didn't have a {nameof(CollidableComponent)}");
            }

            _doAfterSystem = EntitySystem.Get<DoAfterSystem>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "range", SharedInteractionSystem.InteractionRange / 1.4f);
            serializer.DataField(ref _climbDelay, "delay", 0.8f);
        }

        bool IDragDropOn.CanDragDropOn(DragDropEventArgs eventArgs)
        {
            string reason;
            bool canVault;

            if (eventArgs.User == eventArgs.Dropped)
                canVault = CanVault(eventArgs.User, eventArgs.Target, out reason);
            else
                canVault = CanVault(eventArgs.User, eventArgs.Dropped, eventArgs.Target, out reason);

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

            if (!user.HasComponent<ClimbingComponent>())
            {
                reason = Loc.GetString("You are incapable of climbing!");
                return false;
            }

            var bodyManager = user.GetComponent<BodyManagerComponent>();

            if (bodyManager.GetPartsOfType(BodyPartType.Leg).Count == 0 ||
                bodyManager.GetPartsOfType(BodyPartType.Foot).Count == 0)
            {
                reason = Loc.GetString("You are unable to climb!");
                return false;
            }

            if (!user.InRangeUnobstructed(target, _range))
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

            if (!user.InRangeUnobstructed(target, _range, predicate: Ignored) ||
                !user.InRangeUnobstructed(dragged, _range, predicate: Ignored))
            {
                reason = Loc.GetString("You can't reach there!");
                return false;
            }

            reason = string.Empty;
            return true;
        }

        bool IDragDropOn.DragDropOn(DragDropEventArgs eventArgs)
        {
            if (eventArgs.User == eventArgs.Dropped)
            {
                TryClimb(eventArgs.User);
            }
            else
            {
                TryMoveEntity(eventArgs.User, eventArgs.Dropped);
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

            var result = await _doAfterSystem.DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled && entityToMove.TryGetComponent(out ICollidableComponent body) && body.PhysicsShapes.Count >= 1)
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
            var doAfterEventArgs = new DoAfterEventArgs(user, _climbDelay, default, Owner)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true
            };

            var result = await _doAfterSystem.DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled && user.TryGetComponent(out ICollidableComponent body) && body.PhysicsShapes.Count >= 1)
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
