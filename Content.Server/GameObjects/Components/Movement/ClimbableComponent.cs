using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
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
    [ComponentReference(typeof(SharedClimbableComponent))]
    public class ClimbableComponent : SharedClimbableComponent, IDragDropOn
    {
        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.EnsureComponent(out PhysicsComponent _))
            {
                Logger.Warning($"Entity {Owner.Name} at {Owner.Transform.MapPosition} didn't have a {nameof(PhysicsComponent)}");
            }
        }

        bool IDragDropOn.CanDragDropOn(DragDropEventArgs eventArgs)
        {
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

        bool IDragDropOn.DragDropOn(DragDropEventArgs eventArgs)
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
            TryClimb(entityToMove);
        }

        private async void TryClimb(IEntity user)
        {
            if (!user.TryGetComponent(out ClimbingComponent climbingComponent) || climbingComponent.IsClimbing)
                return;

            await climbingComponent.TryClimb(this);
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
