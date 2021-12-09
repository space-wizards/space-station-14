using Content.Shared.Weapons.Ranged.Barrels.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Weapons.Ranged.Barrels.Visualizers
{
    [UsedImplicitly]
    public sealed class BarrelBoltVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity);
            sprite.LayerSetState(RangedBarrelVisualLayers.Bolt, "bolt-open");
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

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
