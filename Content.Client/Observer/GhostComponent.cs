using Content.Client.UserInterface;
using Content.Shared.Observer;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.Client.Observer
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
    {
        private GhostGui _gui;

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
                        _gui = new GhostGui();
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
    }
}
