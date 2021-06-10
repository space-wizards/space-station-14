using Content.Shared.Weapons.Ranged.Barrels.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Barrels.Visualizers
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
