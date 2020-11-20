using Content.Server.GameObjects.Components.TextureSelect;
using Content.Shared.GameObjects.Components.Bible;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Bible
{
    [RegisterComponent]
    internal class BibleComponent : SharedBibleComponent
    {
        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case TextureSelectComponentMessage msg:
                    {
                        if (Owner.TryGetComponent(out AppearanceComponent appearance))
                        {
                            appearance.SetData(BibleVisuals.Style, msg.SelectedTexture);
                        }
                        break;
                    }
            }
        }
    }
}
