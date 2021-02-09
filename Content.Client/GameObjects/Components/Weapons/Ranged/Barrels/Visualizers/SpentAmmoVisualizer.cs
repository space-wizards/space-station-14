using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Weapons.Ranged.Barrels.Visualizers
{
    [UsedImplicitly]
    public sealed class SpentAmmoVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!component.TryGetData(AmmoVisuals.Spent, out bool spent))
            {
                return;
            }

            sprite.LayerSetState(AmmoVisualLayers.Base, spent ? "spent" : "base");
        }
    }

    public enum AmmoVisualLayers : byte
    {
        Base,
    }
}
