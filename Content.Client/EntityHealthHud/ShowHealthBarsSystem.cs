using Content.Shared.EntityHealthBar;
using Robust.Client.Graphics;

namespace Content.Client.EntityHealthHud
{
    public sealed class ShowHealthBarsSystem : ComponentAddedOverlaySystemBase<ShowHealthBarsComponent>
    {
        [Dependency] private readonly IOverlayManager _overlayMan = default!;

        private EntityHealthBarOverlay _overlay = default!;
        public override void Initialize()
        {
            base.Initialize();

            _overlay = new(EntityManager);
        }

        protected override void OnApplyOverlay(ShowHealthBarsComponent component)
        {
            _overlayMan.AddOverlay(_overlay);
            _overlay.DamageContainers.Clear();
            _overlay.DamageContainers.AddRange(component.DamageContainers);
        }

        protected override void OnRemoveOverlay()
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
