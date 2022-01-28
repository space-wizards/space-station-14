using System;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Prototypes;
using Content.Shared.Interaction;
using Robust.Shared;
using Robust.Shared.Configuration;
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
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        private float MaxUpdateRange;

        protected override void Initialize()
        {
            base.Initialize();
            _configManager.OnValueChanged(CVars.NetMaxUpdateRange, OnRangeChanged, true);
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            _configManager.UnsubValueChanged(CVars.NetMaxUpdateRange, OnRangeChanged);
        }

        private void OnRangeChanged(float obj)
        {
            MaxUpdateRange = obj;
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (message is not BasePerformActionMessage performActionMessage) return;
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.AttachedEntity is not {Valid: true} player || player != Owner) return;
            var attempt = ActionAttempt(performActionMessage, session);
            if (attempt == null) return;

            if (!attempt.TryGetActionState(this, out var actionState) || !actionState.Enabled)
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is not granted to them", _entities.GetComponent<MetaDataComponent>(player).EntityName,
                    attempt);
                return;
            }

            if (actionState.IsOnCooldown(GameTiming))
            {
                Logger.DebugS("action", "user {0} attempted to use" +
                                        " action {1} which is on cooldown", _entities.GetComponent<MetaDataComponent>(player).EntityName,
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
                                                " toggle action {1} to {2}, but it is already toggled {2}", _entities.GetComponent<MetaDataComponent>(player).EntityName,
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
                    if (!EntityManager.EntityExists(targetEntityMsg.Target))
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform target entity action {1} but could not find entity with " +
                                                "provided uid {2}", _entities.GetComponent<MetaDataComponent>(player).EntityName, attempt.Action.Name,
                            targetEntityMsg.Target);
                        return;
                    }
                    if (!CheckRangeAndSetFacing(_entities.GetComponent<TransformComponent>(targetEntityMsg.Target).Coordinates, player)) return;

                    attempt.DoTargetEntityAction(player, targetEntityMsg.Target);
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
            var player = session.AttachedEntity;

            switch (message)
            {
                case PerformActionMessage performActionMessage:
                    if (!ActionManager.TryGet(performActionMessage.ActionType, out var action))
                    {
                        Logger.DebugS("action", "user {0} attempted to perform" +
                                                " unrecognized action {1}", player,
                            performActionMessage.ActionType);
                        return null;
                    }
                    attempt = new ActionAttempt(action);
                    break;
                case PerformItemActionMessage performItemActionMessage:
                    var type = performItemActionMessage.ActionType;
                    if (!ActionManager.TryGet(type, out var itemAction))
                    {
                        Logger.DebugS("action", "user {0} attempted to perform" +
                                                " unrecognized item action {1}",
                            player, type);
                        return null;
                    }

                    var item = performItemActionMessage.Item;
                    if (!EntityManager.EntityExists(item))
                    {
                        Logger.DebugS("action", "user {0} attempted to perform" +
                                                " item action {1} for unknown item {2}",
                            player, type, item);
                        return null;
                    }

                    if (!_entities.TryGetComponent<ItemActionsComponent?>(item, out var actionsComponent))
                    {
                        Logger.DebugS("action", "user {0} attempted to perform" +
                                                " item action {1} for item {2} which has no ItemActionsComponent",
                            player, type, item);
                        return null;
                    }

                    if (actionsComponent.Holder != player)
                    {
                        Logger.DebugS("action", "user {0} attempted to perform" +
                                                " item action {1} for item {2} which they are not holding",
                            player, type, item);
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
                                        " {3} behavior", player, attempt, message.BehaviorType,
                    attempt.Action.BehaviorType);
                return null;
            }

            return attempt;
        }

        private bool CheckRangeAndSetFacing(EntityCoordinates target, EntityUid player)
        {
            // ensure it's within their clickable range
            var targetWorldPos = target.ToMapPos(EntityManager);
            var rangeBox = new Box2(_entities.GetComponent<TransformComponent>(player).WorldPosition, _entities.GetComponent<TransformComponent>(player).WorldPosition)
                .Enlarged(MaxUpdateRange);
            if (!rangeBox.Contains(targetWorldPos))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform target action further than allowed range",
                    _entities.GetComponent<MetaDataComponent>(player).EntityName);
                return false;
            }

            EntitySystem.Get<RotateToFaceSystem>().TryFaceCoordinates(player, targetWorldPos);
            return true;
        }
    }
}
