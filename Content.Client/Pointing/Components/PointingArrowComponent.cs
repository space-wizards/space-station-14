using Content.Shared.Pointing.Components;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Pointing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPointingArrowComponent))]
    public sealed class PointingArrowComponent : SharedPointingArrowComponent
    {
        protected override void Startup()
        {
            base.Startup();

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out SpriteComponent? sprite))
            {
                sprite.DrawDepth = (int) DrawDepth.Overlays;
            }
        }
    }
}
