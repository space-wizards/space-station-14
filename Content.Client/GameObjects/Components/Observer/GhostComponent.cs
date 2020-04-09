using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Observer
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
    {
        private GhostGui _gui;
        private bool _canReturnToBody = true;

        [ViewVariables(VVAccess.ReadOnly)]
        public override bool CanReturnToBody
        {
            get => _canReturnToBody;
            set {}
        }

#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private IComponentManager _componentManager;
#pragma warning restore 649

        public override void OnRemove()
        {
            base.OnRemove();

            _gui?.Dispose();
        }


        private void SetGhostVisibility(bool visibility)
        {
            // So, for now this is a client-side hack... Please, PLEASE someone make this work server-side.
            foreach (var ghost in _componentManager.GetAllComponents(typeof(GhostComponent)))
            {
                if (ghost.Owner.TryGetComponent(out SpriteComponent component))
                    component.Visible = visibility;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out SpriteComponent component))
                component.Visible = _playerManager.LocalPlayer.ControlledEntity?.HasComponent<GhostComponent>() ?? false;
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

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

                    break;

                case PlayerDetachedMsg _:
                    _gui.Parent?.RemoveChild(_gui);
                    SetGhostVisibility(false);
                    break;
            }
        }

        public void SendReturnToBodyMessage() => SendNetworkMessage(new ReturnToBodyComponentMessage());

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is GhostComponentState state)) return;

            _canReturnToBody = state.CanReturnToBody;

            if (Owner == _playerManager.LocalPlayer.ControlledEntity)
            {
                _gui?.Update();
            }

        }
    }
}
