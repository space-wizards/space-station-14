using Content.Shared.GameObjects.Components.Pointing;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using DrawDepth = Content.Shared.GameObjects.DrawDepth;

namespace Content.Client.GameObjects.Components.Pointing
{
    [RegisterComponent]
    public class RoguePointingArrowComponent : SharedRoguePointingArrowComponent
    {
        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.DrawDepth = (int) DrawDepth.Overlays;
            }
        }
    }
}
