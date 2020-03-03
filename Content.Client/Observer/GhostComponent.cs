using Content.Client.UserInterface;
using Content.Shared.Observer;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Client.Observer
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
#pragma warning restore 649

        public override void OnRemove()
        {
            base.OnRemove();

            _gui?.Dispose();
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
                        _gui.Parent?.RemoveChild(_gui);
                    }

                    _gameHud.HandsContainer.AddChild(_gui);
                    break;

                case PlayerDetachedMsg _:
                    _gui.Parent?.RemoveChild(_gui);
                    break;
            }
        }

        public void SendReturnToBodyMessage() => SendNetworkMessage(new ReturnToBodyComponentMessage());

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is GhostComponentState state)) return;

            _canReturnToBody = state.CanReturnToBody;
            _gui?.Update();
        }
    }
}
