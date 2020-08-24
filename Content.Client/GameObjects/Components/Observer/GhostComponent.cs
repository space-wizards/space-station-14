using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Observer
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;

        private GhostGui _gui;

        [ViewVariables(VVAccess.ReadOnly)]
        public bool CanReturnToBody { get; private set; } = true;

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

        public override void HandleMessage(ComponentMessage message, IComponent component)
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
                    _gui.Parent?.RemoveChild(_gui);
                    SetGhostVisibility(false);
                    _isAttached = false;
                    break;
            }
        }

        public void SendReturnToBodyMessage() => SendNetworkMessage(new ReturnToBodyComponentMessage());

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is GhostComponentState state)) return;

            CanReturnToBody = state.CanReturnToBody;

            if (Owner == _playerManager.LocalPlayer.ControlledEntity)
            {
                _gui?.Update();
            }

        }
    }
}
