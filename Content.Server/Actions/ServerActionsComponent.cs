using System;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Prototypes;
using Content.Shared.Interaction.Events;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Server.Actions
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedActionsComponent))]
    public sealed class ServerActionsComponent : SharedActionsComponent
    {
        [Dependency] private readonly IServerGameStateManager _stateManager = default!;

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (message is not BasePerformActionMessage performActionMessage) return;
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var player = session.AttachedEntity;
            if (player != Owner) return;
            var attempt = ActionAttempt(performActionMessage, session);
            if (attempt == null) return;

            if (!attempt.TryGetActionState(this, out var actionState) || !actionState.Enabled)
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is not granted to them", player.Name,
                    attempt);
                return;
            }

            if (actionState.IsOnCooldown(GameTiming))
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is on cooldown", player.Name,
                    attempt);
                return;
            }

            switch (performActionMessage.BehaviorType)
            {
                case BehaviorType.Instant:
                    attempt.DoInstantAction(player);
                    break;
                case BehaviorType.Toggle:
                    if (performActionMessage is not IToggleActionMessage toggleMsg) return;
                    if (toggleMsg.ToggleOn == actionState.ToggledOn)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " toggle action {1} to {2}, but it is already toggled {2}", player.Name,
                            attempt.Action.Name, toggleMsg.ToggleOn);
                        return;
                    }

                    if (attempt.DoToggleAction(player, toggleMsg.ToggleOn))
                    {
                        attempt.ToggleAction(this, toggleMsg.ToggleOn);
                    }
                    else
                    {
                        // if client predicted the toggle will work, need to reset
                        // that prediction
                        Dirty();
                    }
                    break;
                case BehaviorType.TargetPoint:
                    if (performActionMessage is not ITargetPointActionMessage targetPointMsg) return;
                    if (!CheckRangeAndSetFacing(targetPointMsg.Target, player)) return;
                    attempt.DoTargetPointAction(player, targetPointMsg.Target);
                    break;
                case BehaviorType.TargetEntity:
                    if (performActionMessage is not ITargetEntityActionMessage targetEntityMsg) return;
                    if (!EntityManager.TryGetEntity(targetEntityMsg.Target, out var entity))
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform target entity action {1} but could not find entity with " +
                                                "provided uid {2}", player.Name, attempt.Action.Name,
                            targetEntityMsg.Target);
                        return;
                    }
                    if (!CheckRangeAndSetFacing(entity.Transform.Coordinates, player)) return;

                    attempt.DoTargetEntityAction(player, entity);
                    break;
                case BehaviorType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IActionAttempt? ActionAttempt(BasePerformActionMessage message, ICommonSession session)
        {
            IActionAttempt? attempt;
            switch (message)
            {
                case PerformActionMessage performActionMessage:
                    if (!ActionManager.TryGet(performActionMessage.ActionType, out var action))
                    {
                        Logger.DebugS("action", "user {0} attempted to perform" +
                                                " unrecognized action {1}", session.AttachedEntity,
                            performActionMessage.ActionType);
                        return null;
                    }
                    attempt = new ActionAttempt(action);
                    break;
                case PerformItemActionMessage performItemActionMessage:
                    if (!ActionManager.TryGet(performItemActionMessage.ActionType, out var itemAction))
                    {
                        Logger.DebugS("action", "user {0} attempted to perform" +
                                                " unrecognized item action {1}",
                            session.AttachedEntity, performItemActionMessage.ActionType);
                        return null;
                    }

                    if (!EntityManager.TryGetEntity(performItemActionMessage.Item, out var item))
                    {
                        Logger.DebugS("action", "user {0} attempted to perform" +
                                                " item action {1} for unknown item {2}",
                            session.AttachedEntity, performItemActionMessage.ActionType, performItemActionMessage.Item);
                        return null;
                    }

                    if (!item.TryGetComponent<ItemActionsComponent>(out var actionsComponent))
                    {
                        Logger.DebugS("action", "user {0} attempted to perform" +
                                                " item action {1} for item {2} which has no ItemActionsComponent",
                            session.AttachedEntity, performItemActionMessage.ActionType, item);
                        return null;
                    }

                    if (actionsComponent.Holder != session.AttachedEntity)
                    {
                        Logger.DebugS("action", "user {0} attempted to perform" +
                                                " item action {1} for item {2} which they are not holding",
                            session.AttachedEntity, performItemActionMessage.ActionType, item);
                        return null;
                    }

                    attempt = new ItemActionAttempt(itemAction, item, actionsComponent);
                    break;
                default:
                    return null;
            }

            if (message.BehaviorType != attempt.Action.BehaviorType)
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform action {1} as a {2} behavior, but this action is actually a" +
                                        " {3} behavior", session.AttachedEntity, attempt, message.BehaviorType,
                    attempt.Action.BehaviorType);
                return null;
            }

            return attempt;
        }

        private bool CheckRangeAndSetFacing(EntityCoordinates target, IEntity player)
        {
            // ensure it's within their clickable range
            var targetWorldPos = target.ToMapPos(EntityManager);
            var rangeBox = new Box2(player.Transform.WorldPosition, player.Transform.WorldPosition)
                .Enlarged(_stateManager.PvsRange);
            if (!rangeBox.Contains(targetWorldPos))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform target action further than allowed range",
                    player.Name);
                return false;
            }

            if (!EntitySystem.Get<ActionBlockerSystem>().CanChangeDirection(player)) return true;

            // don't set facing unless they clicked far enough away
            var diff = targetWorldPos - player.Transform.WorldPosition;
            if (diff.LengthSquared > 0.01f)
            {
                player.Transform.LocalRotation = Angle.FromWorldVec(diff);
            }

            return true;
        }
    }
}
