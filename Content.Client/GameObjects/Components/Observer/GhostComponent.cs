using System.Collections.Generic;
using Content.Client.Interfaces.Chat;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Observer
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public List<string> WarpNames = new();
        public Dictionary<EntityUid,string> PlayerNames = new();

        private GhostGui? _gui ;

        [ViewVariables(VVAccess.ReadOnly)] public bool CanReturnToBody { get; private set; } = true;

        private bool _isAttached;

        public override void OnRemove()
        {
            base.OnRemove();

            _gui?.Dispose();

            // PlayerDetachedMsg might not fire due to deletion order so...
            if (_isAttached)
            {
                SetGhostVisibility(false);
            }
        }

        private void SetGhostVisibility(bool visibility)
        {
            foreach (var ghost in _componentManager.GetAllComponents(typeof(GhostComponent), true))
            {
                if (ghost.Owner.TryGetComponent(out SpriteComponent? component))
                {
                    component.Visible = visibility;
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out SpriteComponent? component))
            {
                component.Visible =
                    _playerManager.LocalPlayer?.ControlledEntity?.HasComponent<GhostComponent>() ?? false;
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg _:
                    if (_gui == null)
                    {
                        _gui = new GhostGui(this);
                    }
                    else
                    {
                        _gui.Orphan();
                    }

                    _gameHud.HandsContainer.AddChild(_gui);
                    SetGhostVisibility(true);
                    _isAttached = true;

                    break;

                case PlayerDetachedMsg _:
                    _gui!.Parent?.RemoveChild(_gui);
                    SetGhostVisibility(false);
                    _isAttached = false;
                    break;
            }
        }

        public void SendReturnToBodyMessage() => SendNetworkMessage(new ReturnToBodyComponentMessage());

        public void SendGhostWarpRequestMessage(string warpName) => SendNetworkMessage(new GhostWarpToLocationRequestMessage(warpName));

        public void SendGhostWarpRequestMessage(EntityUid target) => SendNetworkMessage(new GhostWarpToTargetRequestMessage(target));

        public void GhostRequestWarpPoint() => SendNetworkMessage(new GhostRequestWarpPointData());

        public void GhostRequestPlayerNames() => SendNetworkMessage(new GhostRequestPlayerNameData());

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not GhostComponentState state) return;

            CanReturnToBody = state.CanReturnToBody;

            if (Owner == _playerManager.LocalPlayer!.ControlledEntity)
            {
                _gui?.Update();
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case GhostReplyWarpPointData data:
                    WarpNames = new List<string>();
                    foreach (var names in data.WarpName)
                    {
                        WarpNames.Add(names);
                    }
                    break;
                case GhostReplyPlayerNameData data:
                    PlayerNames = new Dictionary<EntityUid, string>();
                    foreach (var (key, value) in data.PlayerNames)
                    {
                        PlayerNames.Add(key,value);
                    }
                    break;
            }
        }
    }
}
