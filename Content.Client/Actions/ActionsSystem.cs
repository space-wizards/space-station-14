using Content.Client.Outline;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;

namespace Content.Client.Actions
{
    [UsedImplicitly]
    public sealed class ActionsSystem : SharedActionsSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly InteractionOutlineSystem _interactionOutline = default!;
        [Dependency] private readonly TargetOutlineSystem _targetOutline = default!;

        public event Action<ActionType>? OnActionAdded;
        public event Action<ActionType>? OnActionRemoved;
        public event Action<ActionsComponent>? OnLinkActions;
        public event Action? OnUnlinkActions;

        private ActionsComponent? _playerActions;
        public ActionsComponent? PlayerActions => _playerActions;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActionsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ActionsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<ActionsComponent, ComponentHandleState>(HandleComponentState);
        }

        private void HandleComponentState(EntityUid uid, ActionsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ActionsComponentState currentState)
                return;

            List<ActionType> added = new();
            List<ActionType> removed = new();

            foreach (var actionType in component.Actions)
            {
                if (!currentState.Actions.Contains(actionType))
                {
                    removed.Add(actionType);
                }
            }

            foreach (var serverAction in currentState.Actions)
            {
                if (!component.Actions.TryGetValue(serverAction, out var clientAction))
                {
                    added.Add(serverAction);
                }
                else
                {
                    clientAction.CopyFrom(serverAction);
                }
            }

            foreach (var actionType in added)
            {
                component.Actions.Add(actionType);
                OnActionAdded?.Invoke(actionType);
            }

            foreach (var actionType in removed)
            {
                component.Actions.Remove(actionType);
                OnActionRemoved?.Invoke(actionType);
            }
        }

        private void OnPlayerAttached(EntityUid uid, ActionsComponent component, PlayerAttachedEvent args)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity) return;
            OnLinkActions?.Invoke(component);
            _playerActions = component;
        }

        private void OnPlayerDetached(EntityUid uid, ActionsComponent component, PlayerDetachedEvent? args = null)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity) return;
            OnUnlinkActions?.Invoke();
            _playerActions = null;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            CommandBinds.Unregister<ActionsSystem>();
        }

        public void TriggerAction(ActionType? action)
        {
            if (_playerActions == null || action == null || _playerManager.LocalPlayer?.ControlledEntity is not { Valid: true } user)
                return;

            if (action.Provider != null && Deleted(action.Provider))
                return;

            if (action is not InstantAction instantAction)
            {
                return;
            }

            if (action.ClientExclusive)
            {
                if (instantAction.Event != null)
                    instantAction.Event.Performer = user;

                PerformAction(_playerActions, instantAction, instantAction.Event, GameTiming.CurTime);
            }
            else
            {
                var request = new RequestPerformActionEvent(instantAction);
                EntityManager.RaisePredictiveEvent(request);
            }
        }
    }
}
