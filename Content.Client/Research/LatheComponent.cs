using Content.Shared.GameObjects.Components.Research;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;

namespace Content.Client.Research
{
    public class LatheComponent : SharedLatheComponent
    {
        private LatheMenu menu;

        public override void Initialize()
        {
            base.Initialize();
            menu = new LatheMenu {Owner = this};
            menu.AddToScreen();
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                case LatheMenuOpenMessage msg:
                    menu.OpenCentered();
                    break;
            }
        }

        public override void OnRemove()
        {
            menu?.Dispose();
        }
    }
}
