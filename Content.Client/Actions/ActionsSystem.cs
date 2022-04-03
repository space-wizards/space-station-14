using Content.Client.Outline;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Serialization.Manager;

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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActionsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ActionsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<ActionsComponent, ComponentHandleState>(HandleComponentState);
        }

        private void HandleComponentState(EntityUid uid, ActionsComponent component, ref ComponentHandleState args)
        {

        }
        private void OnPlayerAttached(EntityUid uid, ActionsComponent component, PlayerAttachedEvent args)
        {
            if (uid == _playerManager.LocalPlayer?.ControlledEntity) OnLinkActions?.Invoke(component);
        }

        private void OnPlayerDetached(EntityUid uid, ActionsComponent component, PlayerDetachedEvent? args = null)
        {
            if (uid == _playerManager.LocalPlayer?.ControlledEntity) OnUnlinkActions?.Invoke();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            CommandBinds.Unregister<ActionsSystem>();
        }
    }
}
