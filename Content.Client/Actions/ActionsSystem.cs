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
        //public Action? EntityActionUpdate = null; //Unneeded?
        public Action<ActionType>? OnActionAdded = null;
        public Action<ActionType>? OnActionRemoved = null;
        public Action<ActionsComponent>? OnLinkActions = null;
        public Action? OnUnlinkActions = null;
        private ActionsComponent? _playerActions = null;
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
            //Check if player is local
            if (args.Current is not ActionsComponentState currentState) return;
            List<ActionType> _addedTypes = new();
            List<ActionType> _removedTypes = new();
            foreach (var actionType in component.Actions)
            {
                if (!currentState.Actions.Contains(actionType))
                {
                    _removedTypes.Add(actionType);
                }
            }
            foreach (var actionType in currentState.Actions)
            {
                if (!component.Actions.Contains(actionType))
                {
                    _addedTypes.Add(actionType);
                }
            }
            foreach (var actionType in _addedTypes)
            {
                component.Actions.Add(actionType);
                OnActionAdded?.Invoke(actionType);
            }
            foreach (var actionType in _removedTypes)
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
    }
}
