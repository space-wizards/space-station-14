using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Research;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.Client.Research
{
    public class LatheComponent : SharedLatheComponent
    {

        private LatheMenu menu;

        public override void Initialize()
        {
            base.Initialize();
            menu = new LatheMenu(IoCManager.Resolve<IDisplayManager>()) {Owner = this};
            menu.AddToScreen();
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                case LatheMenuOpenMessage msg:
                    menu.OpenCentered();
                    break;
                case LatheMaterialsUpdateMessage msg:
                    _materialStorage = msg.MaterialStorage;
                    break;
            }
        }

        public override void OnRemove()
        {
            menu?.Dispose();
        }
    }
}
