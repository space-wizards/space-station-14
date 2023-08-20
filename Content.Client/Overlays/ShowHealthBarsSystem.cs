using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using System.Linq;

namespace Content.Client.Overlays
{
    public sealed class ShowHealthBarsSystem : EquipmentHudSystem<ShowHealthBarsComponent>
    {
        [Dependency] private readonly IOverlayManager _overlayMan = default!;

        private EntityHealthBarOverlay _overlay = default!;

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new(EntityManager);
        }

        protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowHealthBarsComponent> component)
        {
            base.UpdateInternal(component);

            foreach (var damageContainerId in component.Components.SelectMany(x => x.DamageContainers))
            {
                if (_overlay.DamageContainers.Contains(damageContainerId))
                {
                    continue;
                }

                _overlay.DamageContainers.Add(damageContainerId);
            }

            _overlayMan.AddOverlay(_overlay);
        }

        protected override void DeactivateInternal()
        {
            base.DeactivateInternal();

            _overlay.DamageContainers.Clear();
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
