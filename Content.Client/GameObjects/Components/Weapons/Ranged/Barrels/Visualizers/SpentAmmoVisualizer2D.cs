using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Weapons.Ranged.Barrels.Visualizers
{
    [UsedImplicitly]
    public sealed class SpentAmmoVisualizer2D : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!component.TryGetData(AmmoVisuals.Spent, out bool spent))
            {
                return;
            }

            if (spent)
            {
                sprite.LayerSetState(AmmoVisualLayers.Base, "spent");
            }
            else
            {
                sprite.LayerSetState(AmmoVisualLayers.Base, "base");
            }
        }
    }

    public enum AmmoVisualLayers
    {
        Base,
    }
}