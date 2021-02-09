using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.Weapons.Ranged.Barrels.Visualizers
{
    [UsedImplicitly]
    public sealed class BarrelBoltVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            var sprite = entity.GetComponent<ISpriteComponent>();
            sprite.LayerSetState(RangedBarrelVisualLayers.Bolt, "bolt-open");
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!component.TryGetData(BarrelBoltVisuals.BoltOpen, out bool boltOpen))
            {
                return;
            }

            if (boltOpen)
            {
                sprite.LayerSetState(RangedBarrelVisualLayers.Bolt, "bolt-open");
            }
            else
            {
                sprite.LayerSetState(RangedBarrelVisualLayers.Bolt, "bolt-closed");
            }
        }
    }
}
