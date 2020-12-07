#nullable enable
using System;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedActionsComponent))]
    public sealed class ServerActionsComponent : SharedActionsComponent
    {
        [Dependency] private readonly IServerEntityManager _entityManager = default!;

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (message is PerformActionMessage performMsg)
            {
                HandlePerformActionMessage(performMsg, session);
            }
            else if (message is PerformItemActionMessage performItemActionMsg)
            {
                HandlePerformItemActionMessage(performItemActionMsg, session);
            }
        }

        private void HandlePerformActionMessage(PerformActionMessage performMsg, ICommonSession session)
        {
            var player = session.AttachedEntity;
            if (player != Owner) return;

            if (!TryGetActionState(performMsg.ActionType, out var actionState) || !actionState.Enabled)
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is not granted to them", player.Name,
                    performMsg.ActionType);
                return;
            }

            if (actionState.IsOnCooldown(GameTiming))
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is on cooldown", player.Name,
                    performMsg.ActionType);
                return;
            }

            if (!ActionManager.TryGet(performMsg.ActionType, out var action))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform unrecognized action {1}", player.Name,
                    performMsg.ActionType);
                return;
            }

            switch (performMsg)
            {
                case PerformInstantActionMessage msg:
                    if (action.InstantAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as an instant action, but it isn't one",
                            player.Name,
                            msg.ActionType);
                        return;
                    }

                    action.InstantAction.DoInstantAction(new InstantActionEventArgs(player, action.ActionType));

                    break;
                case PerformToggleOnActionMessage:
                    HandleToggleAction(action, player, true, actionState);
                    break;
                case PerformToggleOffActionMessage:
                    HandleToggleAction(action, player, false, actionState);
                    break;
                case PerformTargetPointActionMessage msg:
                    if (action.TargetPointAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a target point action, but it isn't one",
                            player.Name, msg.ActionType);
                        return;
                    }

                    if (!CheckRangeAndSetFacing(msg.Target, player)) return;

                    action.TargetPointAction.DoTargetPointAction(
                        new TargetPointActionEventArgs(player, msg.Target, action.ActionType));
                    break;
                case PerformTargetEntityActionMessage msg:
                    if (action.TargetEntityAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a target entity action, but it isn't one",
                            player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (!EntityManager.TryGetEntity(msg.Target, out var entity))
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform target entity action {1} but could not find entity with " +
                                                "provided uid {2}", player.Name, msg.ActionType, msg.Target);
                        return;
                    }

                    if (!CheckRangeAndSetFacing(entity.Transform.Coordinates, player)) return;

                    action.TargetEntityAction.DoTargetEntityAction(
                        new TargetEntityActionEventArgs(player, action.ActionType, entity));
                    break;
            }
        }

        private void HandleToggleAction(ActionPrototype action, IEntity player, bool on,
            ActionState actionState)
        {
            if (action.ToggleAction == null)
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform action {1} as a toggle action, but it isn't one", player.Name,
                    action.ActionType);
                return;
            }

            if (on == actionState.ToggledOn)
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " toggle action {1} to {2}, but it is already toggled {2}", player.Name,
                    action.ActionType, actionState.ToggledOn ? "on" : "off");
                return;
            }

            ToggleAction(action.ActionType, on);

            action.ToggleAction.DoToggleAction(new ToggleActionEventArgs(player, action.ActionType,
                on));
        }

        private void HandlePerformItemActionMessage(PerformItemActionMessage performMsg, ICommonSession session)
        {
            var player = session.AttachedEntity;
            if (player != Owner) return;

            if (!TryGetItemActionState(performMsg.ActionType, performMsg.Item, out var actionState) || !actionState.Enabled)
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is not granted to them", player.Name,
                    performMsg.ActionType);
                return;
            }

            if (actionState.IsOnCooldown(GameTiming))
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is on cooldown", player.Name,
                    performMsg.ActionType);
                return;
            }

            if (!ActionManager.TryGet(performMsg.ActionType, out var action))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform unrecognized action {1}", player.Name,
                    performMsg.ActionType);
                return;
            }

            // item must be in inventory
            if (!IsEquipped(performMsg.Item))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform action {1} but the item for this action ({2}) is not in inventory", player.Name,
                    performMsg.ActionType, performMsg.Item);
                // failsafe, ensure it's revoked (it should've been already)
                Revoke(performMsg.Item);
                return;
            }

            if (!EntityManager.TryGetEntity(performMsg.Item, out var item))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform action {1} but the item for this action ({2}) could not be found", player.Name,
                    performMsg.ActionType, performMsg.Item);
                // failsafe, ensure it's revoked (it should've been already)
                Revoke(performMsg.Item);
                return;
            }

            switch (performMsg)
            {
                case PerformInstantItemActionMessage msg:
                    if (action.InstantAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as an instant item action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    action.InstantAction.DoInstantAction(new InstantItemActionEventArgs(player, item, action.ActionType));

                    break;
                case PerformToggleOnItemActionMessage msg:
                    HandleToggleItemAction(action, player, true, actionState, item);
                    break;
                case PerformToggleOffItemActionMessage msg:
                    HandleToggleItemAction(action, player, false, actionState, item);
                    break;
                case PerformTargetPointItemActionMessage msg:
                    if (action.TargetPointAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a target point item action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (!CheckRangeAndSetFacing(msg.Target, player)) return;

                    action.TargetPointAction.DoTargetPointAction(
                        new TargetPointItemActionEventArgs(player, msg.Target, item, action.ActionType));
                    break;
                case PerformTargetEntityItemActionMessage msg:
                    if (action.TargetEntityAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a target entity action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (!EntityManager.TryGetEntity(msg.Target, out var entity))
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform target entity action {1} but could not find entity with " +
                                                "provided uid {2}", player.Name, msg.ActionType, msg.Target);
                        return;
                    }

                    if (!CheckRangeAndSetFacing(entity.Transform.Coordinates, player)) return;

                    action.TargetEntityAction.DoTargetEntityAction(
                        new TargetEntityItemActionEventArgs(player, entity, item, action.ActionType));
                    break;
            }
        }

        private void HandleToggleItemAction(ItemActionPrototype action, IEntity player, bool on,
            ActionState actionState, IEntity item)
        {
            if (action.ToggleAction == null)
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform action {1} as a toggle item action, but it isn't one", player.Name,
                    action.ActionType);
                return;
            }

            if (on == actionState.ToggledOn)
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " toggle item action {1} to {2}, but it is already toggled {2}", player.Name,
                    action.ActionType, actionState.ToggledOn ? "on" : "off");
                return;
            }

            ToggleAction(action.ActionType, item, on);

            action.ToggleAction.DoToggleAction(new ToggleItemActionEventArgs(player, on, item, action.ActionType));
        }

        private bool CheckRangeAndSetFacing(EntityCoordinates target, IEntity player)
        {
            // ensure it's within their clickable range
            var targetWorldPos = target.ToMapPos(EntityManager);
            var rangeBox = new Box2(player.Transform.WorldPosition, player.Transform.WorldPosition)
                .Enlarged(_entityManager.MaxUpdateRange);
            if (!rangeBox.Contains(targetWorldPos))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform target action further than allowed range",
                    player.Name);
                return false;
            }

            if (!ActionBlockerSystem.CanChangeDirection(player)) return true;

            // don't set facing unless they clicked far enough away
            var diff = targetWorldPos - player.Transform.WorldPosition;
            if (diff.LengthSquared > 0.01f)
            {
                player.Transform.LocalRotation = new Angle(diff);
            }

            return true;
        }
    }
}
