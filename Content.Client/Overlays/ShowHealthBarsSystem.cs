using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Overlays
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
            base.OnApplyOverlay(component);

            foreach (var damageContainerId in component.DamageContainers)
            {
                if (_overlay.DamageContainers.Contains(damageContainerId))
                {
                    continue;
                }

                _overlay.DamageContainers.Add(damageContainerId);
            }

            _overlayMan.AddOverlay(_overlay);
        }

        protected override void OnRemoveOverlay()
        {
            base.OnRemoveOverlay();

            _overlay.DamageContainers.Clear();
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
