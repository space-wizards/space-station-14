using Content.Shared.Weapons.Ranged.Barrels.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Weapons.Ranged.Barrels.Visualizers
{
    [UsedImplicitly]
    public sealed class SpentAmmoVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

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
