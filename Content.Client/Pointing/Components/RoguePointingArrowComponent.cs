using Content.Shared.Pointing.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Pointing.Components
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
